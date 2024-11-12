using Basis.Scripts.BasisSdk.Players;
using Basis.Scripts.TransformBinders.BoneControl;
using Unity.Mathematics;
using UnityEngine;

[System.Serializable]
public class BasisVirtualSpineDriver
{
    [SerializeField] public BasisBoneControl CenterEye;
    [SerializeField] public BasisBoneControl Head;
    [SerializeField] public BasisBoneControl Neck;
    [SerializeField] public BasisBoneControl Chest;
    [SerializeField] public BasisBoneControl Hips;

    [SerializeField] public BasisBoneControl RightShoulder;
    [SerializeField] public BasisBoneControl LeftShoulder;

    [SerializeField] public BasisBoneControl LeftLowerArm;
    [SerializeField] public BasisBoneControl RightLowerArm;

    [SerializeField] public BasisBoneControl LeftLowerLeg;
    [SerializeField] public BasisBoneControl RightLowerLeg;

    [SerializeField] public BasisBoneControl LeftHand;
    [SerializeField] public BasisBoneControl RightHand;

    [SerializeField] public BasisBoneControl LeftFoot;
    [SerializeField] public BasisBoneControl RightFoot;

    // Define influence values (from 0 to 1)
    public float NeckRotationSpeed = 4;
    public float ChestRotationSpeed = 4;
    public float HipsRotationSpeed = 10;
    public float MaxNeckAngle = 0; // Limit the neck's rotation range to avoid extreme twisting
    public float MaxChestAngle = 0; // Limit the chest's rotation range
    public float MaxHipsAngle = 0; // Limit the hips' rotation range
    public float HipsInfluence = 0.5f;

    public float MiddlePointsLerpFactor = 0.5f;
    public void Initialize()
    {
        if (BasisLocalPlayer.Instance.LocalBoneDriver.FindBone(out CenterEye, BasisBoneTrackedRole.CenterEye))
        {
        }
        if (BasisLocalPlayer.Instance.LocalBoneDriver.FindBone(out Head, BasisBoneTrackedRole.Head))
        {
        }

        if (BasisLocalPlayer.Instance.LocalBoneDriver.FindBone(out Neck, BasisBoneTrackedRole.Neck))
        {
            Neck.HasVirtualOverride = true;
            Neck.VirtualRun += OnSimulateNeck;
        }
        if (BasisLocalPlayer.Instance.LocalBoneDriver.FindBone(out Chest, BasisBoneTrackedRole.Chest))
        {
            Chest.HasVirtualOverride = true;
        }
        if (BasisLocalPlayer.Instance.LocalBoneDriver.FindBone(out Hips, BasisBoneTrackedRole.Hips))
        {
            Hips.HasVirtualOverride = true;
        }

        if (BasisLocalPlayer.Instance.LocalBoneDriver.FindBone(out LeftLowerArm, BasisBoneTrackedRole.LeftLowerArm))
        {
            // LeftUpperArm.HasVirtualOverride = true;
            BasisLocalPlayer.Instance.LocalBoneDriver.ReadyToRead.AddAction(28, LowerLeftArm);
        }
        if (BasisLocalPlayer.Instance.LocalBoneDriver.FindBone(out RightLowerArm, BasisBoneTrackedRole.RightLowerArm))
        {
            //   RightUpperArm.HasVirtualOverride = true;
            BasisLocalPlayer.Instance.LocalBoneDriver.ReadyToRead.AddAction(29, LowerLeftLeg);
        }
        if (BasisLocalPlayer.Instance.LocalBoneDriver.FindBone(out LeftLowerLeg, BasisBoneTrackedRole.LeftLowerLeg))
        {
            //  LeftLowerLeg.HasVirtualOverride = true;
            BasisLocalPlayer.Instance.LocalBoneDriver.ReadyToRead.AddAction(30, LowerRightArm);
        }
        if (BasisLocalPlayer.Instance.LocalBoneDriver.FindBone(out RightLowerLeg, BasisBoneTrackedRole.RightLowerLeg))
        {
            //  RightLowerLeg.HasVirtualOverride = true;
            BasisLocalPlayer.Instance.LocalBoneDriver.ReadyToRead.AddAction(31, LowerRightleg);
        }
        if (BasisLocalPlayer.Instance.LocalBoneDriver.FindBone(out LeftHand, BasisBoneTrackedRole.LeftHand))
        {
            // LeftHand.HasVirtualOverride = true;
        }
        if (BasisLocalPlayer.Instance.LocalBoneDriver.FindBone(out RightHand, BasisBoneTrackedRole.RightHand))
        {
            //   RightHand.HasVirtualOverride = true;
        }
        if (BasisLocalPlayer.Instance.LocalBoneDriver.FindBone(out LeftFoot, BasisBoneTrackedRole.LeftFoot))
        {
            // LeftHand.HasVirtualOverride = true;
        }
        if (BasisLocalPlayer.Instance.LocalBoneDriver.FindBone(out RightFoot, BasisBoneTrackedRole.RightFoot))
        {
            //   RightHand.HasVirtualOverride = true;
        }
    }
    public void LowerLeftLeg()
    {
        LeftLowerLeg.BoneTransform.position = LeftFoot.BoneTransform.position;
    }
    public void LowerRightleg()
    {
        RightLowerLeg.BoneTransform.position = RightFoot.BoneTransform.position;
    }
    public void LowerLeftArm()
    {
        LeftLowerArm.BoneTransform.position = LeftHand.BoneTransform.position;
    }
    public void LowerRightArm()
    {
        RightLowerArm.BoneTransform.position = RightHand.BoneTransform.position;
    }
    public float JointSpeedup = 10f;
    public float SmoothTime = 0.1f; // Adjust for smoother damping
    private Vector3 velocity = Vector3.zero;
    public void DeInitialize()
    {
        Neck.VirtualRun -= OnSimulateNeck;
        Neck.HasVirtualOverride = false;
        Chest.HasVirtualOverride = false;
        Hips.HasVirtualOverride = false;
    }
    public Quaternion targetNeckRotation;
    public Quaternion NeckOutput;
    public Quaternion ChestOutput;
    public float3 pelvisRotation;

    public float3 HeadRotation;
    public void OnSimulateNeck()
    {
        float time = BasisLocalPlayer.Instance.LocalBoneDriver.DeltaTime;

        // Lock pelvis Y rotation to head Y rotation, but keep the X and Z rotations of pelvis intact
        Quaternion HipsRotationConverted = Hips.OutGoingData.rotation;
        Vector3 pelvisRotationYOnly = HipsRotationConverted.eulerAngles;

        Quaternion Rotation = Head.OutGoingData.rotation;
        Quaternion HipsRotation = Quaternion.Euler(pelvisRotationYOnly.x, Rotation.eulerAngles.y, pelvisRotationYOnly.z);

        Hips.OutGoingData.rotation = Quaternion.Slerp(Hips.OutGoingData.rotation, HipsRotationConverted, time * HipsRotationSpeed);

        // Calculate the desired rotation for the neck, with limits
        Quaternion HeadRotationConverted = Head.OutGoingData.rotation;
        float3 HeadRotationVector = HeadRotationConverted.eulerAngles;

        // Smooth the neck rotation and clamp it to prevent unnatural flipping
        float clampedHeadPitch = Mathf.Clamp(HeadRotationVector.x, -MaxNeckAngle, MaxNeckAngle);
        Quaternion targetNeckRotation = Quaternion.Euler(clampedHeadPitch, HeadRotationVector.y, 0);

        // Smooth transition for the neck to follow the head
        Neck.OutGoingData.rotation = Quaternion.Slerp(Neck.OutGoingData.rotation, targetNeckRotation, time * NeckRotationSpeed);

        // Clamp the neck's final rotation to avoid excessive twisting
        Quaternion NeckRotation = Neck.OutGoingData.rotation;
        float3 VectorNeckRotation = NeckRotation.eulerAngles;
        float clampedNeckPitch = Mathf.Clamp(VectorNeckRotation.x, -MaxNeckAngle, MaxNeckAngle);
        Neck.OutGoingData.rotation = Quaternion.Euler(clampedNeckPitch, VectorNeckRotation.y, 0);

        // Now, apply the spine curve progressively:
        // The chest should not follow the head directly, it should follow the neck but with reduced influence.
        Quaternion targetChestRotation = Quaternion.Slerp(Chest.OutGoingData.rotation, Neck.OutGoingData.rotation, time * ChestRotationSpeed);

        // Clamp the chest's rotation to avoid unnatural bending
        Vector3 EulerAngleChest = targetChestRotation.eulerAngles;
        float clampedChestPitch = Mathf.Clamp(EulerAngleChest.x, -MaxChestAngle, MaxChestAngle);
        Chest.OutGoingData.rotation = Quaternion.Euler(clampedChestPitch, EulerAngleChest.y, 0);

        // The hips should stay upright, using chest rotation as a reference
        Quaternion targetHipsRotation = Quaternion.Slerp(Hips.OutGoingData.rotation, Chest.OutGoingData.rotation, time * HipsInfluence);// Lesser influence for hips to remain more upright

        // Clamp the hips' rotation to prevent flipping
        Vector3 HipsTargetRotation = targetHipsRotation.eulerAngles;
        float clampedHipsPitch = Mathf.Clamp(HipsTargetRotation.x, -MaxHipsAngle, MaxHipsAngle);
        Hips.OutGoingData.rotation = Quaternion.Euler(clampedHipsPitch, HipsTargetRotation.y, 0);

        // Handle position control for each segment if targets are set (as before)
        ApplyPositionControl(Hips);
        ApplyPositionControl(Neck);
        ApplyPositionControl(Chest);
    }
    private void ApplyPositionControl(BasisBoneControl boneControl)
    {
        if (boneControl.PositionControl.HasTarget)
        {
            // Convert Offset to float3
            float3 offset = boneControl.PositionControl.Offset;

            // Use math.mul to apply the quaternion rotation to the offset vector
            float3 customDirection = math.mul(boneControl.PositionControl.Target.OutGoingData.rotation, offset);

            // Calculate the final position by adding the customDirection to the target position
            boneControl.OutGoingData.position = boneControl.PositionControl.Target.OutGoingData.position + customDirection;
        }
    }
}