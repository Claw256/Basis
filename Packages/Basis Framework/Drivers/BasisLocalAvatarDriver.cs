using Basis.Scripts.Animator_Driver;
using Basis.Scripts.BasisSdk.Helpers;
using Basis.Scripts.BasisSdk.Players;
using Basis.Scripts.Common.Enums;
using Basis.Scripts.Eye_Follow;
using Basis.Scripts.TransformBinders.BoneControl;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Animations.Rigging;

namespace Basis.Scripts.Drivers
{
    public class BasisLocalAvatarDriver : BasisAvatarDriver
    {
        public Vector3 HeadScale;
        public Vector3 HeadScaledDown;
        public BasisLocalBoneDriver LocalDriver;
        public BasisLocalAnimatorDriver AnimatorDriver;
        public BasisLocalPlayer LocalPlayer;
        public TwoBoneIKConstraint HeadTwoBoneIK;
        public TwoBoneIKConstraint LeftFootTwoBoneIK;
        public TwoBoneIKConstraint RightFootTwoBoneIK;
        public TwoBoneIKConstraint LeftHandTwoBoneIK;
        public TwoBoneIKConstraint RightHandTwoBoneIK;
        public TwoBoneIKConstraint UpperChestTwoBoneIK;
        public TwoBoneIKConstraint LeftShoulderTwoBoneIK;
        public TwoBoneIKConstraint RightShoulderTwoBoneIK;
        [SerializeField]
        public List<TwoBoneIKConstraint> LeftFingers = new List<TwoBoneIKConstraint>();
        [SerializeField]
        public List<TwoBoneIKConstraint> RightFingers = new List<TwoBoneIKConstraint>();
        public Rig LeftToeRig;
        public Rig RightToeRig;

        public Rig RigHeadRig;
        public Rig LeftHandRig;
        public Rig RightHandRig;
        public Rig LeftFootRig;
        public Rig RightFootRig;
        public Rig ChestSpineRig;
        public Rig LeftShoulderRig;
        public Rig RightShoulderRig;

        public RigLayer LeftHandLayer;
        public RigLayer RightHandLayer;
        public RigLayer LeftFootLayer;
        public RigLayer RightFootLayer;
        public RigLayer LeftToeLayer;
        public RigLayer RightToeLayer;

        public RigLayer RigHeadLayer;
        public RigLayer ChestSpineLayer;

        public RigLayer LeftShoulderLayer;
        public RigLayer RightShoulderLayer;
        public List<Rig> Rigs = new List<Rig>();
        public RigBuilder Builder;
        public List<RigTransform> AdditionalTransforms = new List<RigTransform>();
        public bool HasTposeEvent = false;
        public string Locomotion = "Locomotion";
        public BasisMuscleDriver BasisMuscleDriver;
        public BasisLocalEyeFollowDriver BasisLocalEyeFollowDriver;
        public void LocalCalibration()
        {
            InitialLocalCalibration(BasisLocalPlayer.Instance);
        }
        public void InitialLocalCalibration(BasisLocalPlayer Player)
        {
            Debug.Log("InitialLocalCalibration");
            LocalPlayer = Player;
            this.LocalDriver = LocalPlayer.LocalBoneDriver;
            if (IsAble())
            {
                // Debug.Log("LocalCalibration Underway");
            }
            else
            {
                return;
            }
            CleanupBeforeContinue();
            AdditionalTransforms.Clear();
            Rigs.Clear();
            if(Player.Avatar.Animator.runtimeAnimatorController == null)
            {
                UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationHandle<RuntimeAnimatorController> op = Addressables.LoadAssetAsync<RuntimeAnimatorController>(Locomotion);
                RuntimeAnimatorController RAC = op.WaitForCompletion();
                Player.Avatar.Animator.runtimeAnimatorController = RAC;
            }
            PutAvatarIntoTPose();
            if (Builder != null)
            {
                GameObject.Destroy(Builder);
            }
            Builder = BasisHelpers.GetOrAddComponent<RigBuilder>(Player.Avatar.Animator.gameObject);
            Calibration(Player.Avatar);
            BasisLocalPlayer.Instance.LocalBoneDriver.RemoveAllListeners();
            BasisLocalPlayer.Instance.LocalBoneDriver.CalibrateOffsets();
            BasisLocalEyeFollowDriver = BasisHelpers.GetOrAddComponent<BasisLocalEyeFollowDriver>(Player.Avatar.gameObject);
            BasisLocalEyeFollowDriver.Initalize(this);
            HeadScaledDown = Vector3.zero;
            SetAllMatrixRecalculation(true);
            updateWhenOffscreen(true);
            if (References.Hashead)
            {
                HeadScale = References.head.localScale;
            }
            else
            {
                HeadScale = Vector3.one;
            }
            SetBodySettings(LocalDriver);
            CalculateTransformPositions(Player.Avatar.Animator, LocalDriver);
            ComputeOffsets(LocalDriver);
            Builder.Build();
            CalibrationComplete?.Invoke();

            BasisMuscleDriver = BasisHelpers.GetOrAddComponent<BasisMuscleDriver>(Player.Avatar.Animator.gameObject);
            BasisMuscleDriver.Initialize(this,Player.Avatar.Animator);

            AnimatorDriver = BasisHelpers.GetOrAddComponent<BasisLocalAnimatorDriver>(Player.Avatar.Animator.gameObject);
            AnimatorDriver.Initialize(Player.Avatar.Animator);

            ResetAvatarAnimator();

            if (BasisLocalPlayer.Instance.LocalBoneDriver.FindBone(out BasisBoneControl Head, BasisBoneTrackedRole.Head))
            {
                Head.HasRigLayer = BasisHasRigLayer.HasRigLayer;
            }
            if (BasisLocalPlayer.Instance.LocalBoneDriver.FindBone(out BasisBoneControl Hips, BasisBoneTrackedRole.Hips))
            {
                Hips.HasRigLayer = BasisHasRigLayer.HasRigLayer;
            }
            if (BasisLocalPlayer.Instance.LocalBoneDriver.FindBone(out BasisBoneControl Chest, BasisBoneTrackedRole.Chest))
            {
                Chest.HasRigLayer = BasisHasRigLayer.HasRigLayer;
            }
            if (BasisLocalPlayer.Instance.LocalBoneDriver.FindBone(out BasisBoneControl Spine, BasisBoneTrackedRole.Spine))
            {
                Spine.HasRigLayer = BasisHasRigLayer.HasRigLayer;
            }
            if (HasTposeEvent == false)
            {
                TposeStateChange += OnTpose;
                HasTposeEvent = true;
            }

            if (Builder.enabled == false)
            {
                Builder.enabled = true;
            }
        }
        public void OnTpose()
        {
            if (Builder != null)
            {
                if (InTPose)
                {
                    Builder.enabled = false;
                }
                else
                {
                    Builder.enabled = true;
                }
            }
        }
        public void CleanupBeforeContinue()
        {
            if (Builder != null)
            {
                Destroy(Builder);
            }
            Builder = null;
            if (RigHeadRig != null)
            {
                Destroy(RigHeadRig.gameObject);
            }
            if (LeftHandRig != null)
            {
                Destroy(LeftHandRig.gameObject);
            }
            if (RightHandRig != null)
            {
                Destroy(RightHandRig.gameObject);
            }
            if (LeftFootRig != null)
            {
                Destroy(LeftFootRig.gameObject);
            }
            if (RightFootRig != null)
            {
                Destroy(RightFootRig.gameObject);
            }
            if (ChestSpineRig != null)
            {
                Destroy(ChestSpineRig.gameObject);
            }
            if (LeftShoulderRig != null)
            {
                Destroy(LeftShoulderRig.gameObject);
            }
            if (RightShoulderRig != null)
            {
                Destroy(RightShoulderRig.gameObject);
            }

            if (LeftToeRig != null)
            {
                Destroy(LeftToeRig.gameObject);
            }
            if (RightToeRig != null)
            {
                Destroy(RightToeRig.gameObject);
            }
        }
        public void ComputeOffsets(BaseBoneDriver BaseBoneDriver)
        {
            //head
            SetAndCreateLock(BaseBoneDriver, BasisBoneTrackedRole.CenterEye, BasisBoneTrackedRole.Head, BasisTargetController.TargetDirectional, 40, BasisClampData.None, 5, 20, true, 5f);
            SetAndCreateLock(BaseBoneDriver, BasisBoneTrackedRole.Head, BasisBoneTrackedRole.Neck, BasisTargetController.TargetDirectional, 40, BasisClampData.Clamp, 5, 20, true, 4, BasisTargetController.Target, BasisClampAxis.xz);

            SetAndCreateLock(BaseBoneDriver, BasisBoneTrackedRole.Head, BasisBoneTrackedRole.Mouth, BasisTargetController.TargetDirectional, 40, BasisClampData.None, 0, 20, false, 4);


            SetAndCreateLock(BaseBoneDriver, BasisBoneTrackedRole.Neck, BasisBoneTrackedRole.Chest, BasisTargetController.TargetDirectional, 40, BasisClampData.None, 0, 20, true, 4);
            SetAndCreateLock(BaseBoneDriver, BasisBoneTrackedRole.Chest, BasisBoneTrackedRole.Spine, BasisTargetController.TargetDirectional, 40, BasisClampData.None, 0, 7, true, 4);
            SetAndCreateLock(BaseBoneDriver, BasisBoneTrackedRole.Spine, BasisBoneTrackedRole.Hips, BasisTargetController.TargetDirectional, 40, BasisClampData.None, 0, 7, true, 4);

            SetAndCreateLock(BaseBoneDriver, BasisBoneTrackedRole.Chest, BasisBoneTrackedRole.LeftShoulder, BasisTargetController.TargetDirectional, 40, BasisClampData.None, 0, 7, false, 4);
            SetAndCreateLock(BaseBoneDriver, BasisBoneTrackedRole.Chest, BasisBoneTrackedRole.RightShoulder, BasisTargetController.TargetDirectional, 40, BasisClampData.None, 0, 7, false, 4);

            SetAndCreateLock(BaseBoneDriver, BasisBoneTrackedRole.LeftShoulder, BasisBoneTrackedRole.LeftUpperArm, BasisTargetController.TargetDirectional, 40, BasisClampData.None, 0, 7, false, 4);
            SetAndCreateLock(BaseBoneDriver, BasisBoneTrackedRole.RightShoulder, BasisBoneTrackedRole.RightUpperArm, BasisTargetController.TargetDirectional, 40, BasisClampData.None, 0, 7, false, 4);

            SetAndCreateLock(BaseBoneDriver, BasisBoneTrackedRole.LeftUpperArm, BasisBoneTrackedRole.LeftLowerArm, BasisTargetController.TargetDirectional, 40, BasisClampData.None, 0, 7, false, 4);
            SetAndCreateLock(BaseBoneDriver, BasisBoneTrackedRole.RightUpperArm, BasisBoneTrackedRole.RightLowerArm, BasisTargetController.TargetDirectional, 40, BasisClampData.None, 0, 7, false, 4);

            SetAndCreateLock(BaseBoneDriver, BasisBoneTrackedRole.LeftLowerArm, BasisBoneTrackedRole.LeftHand, BasisTargetController.TargetDirectional, 40, BasisClampData.None, 0, 7, false, 4);
            SetAndCreateLock(BaseBoneDriver, BasisBoneTrackedRole.RightLowerArm, BasisBoneTrackedRole.RightHand, BasisTargetController.TargetDirectional, 40, BasisClampData.None, 0, 7, false, 4);

            //legs
            SetAndCreateLock(BaseBoneDriver, BasisBoneTrackedRole.Hips, BasisBoneTrackedRole.LeftUpperLeg, BasisTargetController.TargetDirectional, 40, BasisClampData.None, 0, 7, false, 4);
            SetAndCreateLock(BaseBoneDriver, BasisBoneTrackedRole.Hips, BasisBoneTrackedRole.RightUpperLeg, BasisTargetController.TargetDirectional, 40, BasisClampData.None, 0, 7, false, 4);

            SetAndCreateLock(BaseBoneDriver, BasisBoneTrackedRole.LeftUpperLeg, BasisBoneTrackedRole.LeftLowerLeg, BasisTargetController.TargetDirectional, 40, BasisClampData.None, 0, 7, false, 4);
            SetAndCreateLock(BaseBoneDriver, BasisBoneTrackedRole.RightUpperLeg, BasisBoneTrackedRole.RightLowerLeg, BasisTargetController.TargetDirectional, 40, BasisClampData.None, 0, 7, false, 4);

            SetAndCreateLock(BaseBoneDriver, BasisBoneTrackedRole.LeftLowerLeg, BasisBoneTrackedRole.LeftFoot, BasisTargetController.TargetDirectional, 40, BasisClampData.None, 0, 7, false, 4);
            SetAndCreateLock(BaseBoneDriver, BasisBoneTrackedRole.RightLowerLeg, BasisBoneTrackedRole.RightFoot, BasisTargetController.TargetDirectional, 40, BasisClampData.None, 0, 7, false, 4);

            SetAndCreateLock(BaseBoneDriver, BasisBoneTrackedRole.LeftFoot, BasisBoneTrackedRole.LeftToes, BasisTargetController.TargetDirectional, 40, BasisClampData.None, 0, 7, false, 4);
            SetAndCreateLock(BaseBoneDriver, BasisBoneTrackedRole.RightFoot, BasisBoneTrackedRole.RightToes, BasisTargetController.TargetDirectional, 40, BasisClampData.None, 0, 7, false, 4);



            // Setting up locks for Left Hand
            SetAndCreateLock(BaseBoneDriver, BasisBoneTrackedRole.LeftHand, BasisBoneTrackedRole.LeftThumbProximal, BasisTargetController.TargetDirectional, 40, BasisClampData.None, 0, 7, false, 4);
            SetAndCreateLock(BaseBoneDriver, BasisBoneTrackedRole.LeftThumbProximal, BasisBoneTrackedRole.LeftThumbIntermediate, BasisTargetController.TargetDirectional, 40, BasisClampData.None, 0, 7, false, 4);
            SetAndCreateLock(BaseBoneDriver, BasisBoneTrackedRole.LeftThumbIntermediate, BasisBoneTrackedRole.LeftThumbDistal, BasisTargetController.TargetDirectional, 40, BasisClampData.None, 0, 7, false, 4);

            SetAndCreateLock(BaseBoneDriver, BasisBoneTrackedRole.LeftHand, BasisBoneTrackedRole.LeftIndexProximal, BasisTargetController.TargetDirectional, 40, BasisClampData.None, 0, 7, false, 4);
            SetAndCreateLock(BaseBoneDriver, BasisBoneTrackedRole.LeftIndexProximal, BasisBoneTrackedRole.LeftIndexIntermediate, BasisTargetController.TargetDirectional, 40, BasisClampData.None, 0, 7, false, 4);
            SetAndCreateLock(BaseBoneDriver, BasisBoneTrackedRole.LeftIndexIntermediate, BasisBoneTrackedRole.LeftIndexDistal, BasisTargetController.TargetDirectional, 40, BasisClampData.None, 0, 7, false, 4);

            SetAndCreateLock(BaseBoneDriver, BasisBoneTrackedRole.LeftHand, BasisBoneTrackedRole.LeftMiddleProximal, BasisTargetController.TargetDirectional, 40, BasisClampData.None, 0, 7, false, 4);
            SetAndCreateLock(BaseBoneDriver, BasisBoneTrackedRole.LeftMiddleProximal, BasisBoneTrackedRole.LeftMiddleIntermediate, BasisTargetController.TargetDirectional, 40, BasisClampData.None, 0, 7, false, 4);
            SetAndCreateLock(BaseBoneDriver, BasisBoneTrackedRole.LeftMiddleIntermediate, BasisBoneTrackedRole.LeftMiddleDistal, BasisTargetController.TargetDirectional, 40, BasisClampData.None, 0, 7, false, 4);

            SetAndCreateLock(BaseBoneDriver, BasisBoneTrackedRole.LeftHand, BasisBoneTrackedRole.LeftRingProximal, BasisTargetController.TargetDirectional, 40, BasisClampData.None, 0, 7, false, 4);
            SetAndCreateLock(BaseBoneDriver, BasisBoneTrackedRole.LeftRingProximal, BasisBoneTrackedRole.LeftRingIntermediate, BasisTargetController.TargetDirectional, 40, BasisClampData.None, 0, 12, false, 4);
            SetAndCreateLock(BaseBoneDriver, BasisBoneTrackedRole.LeftRingIntermediate, BasisBoneTrackedRole.LeftRingDistal, BasisTargetController.TargetDirectional, 40, BasisClampData.None, 0, 12, false, 4);

            SetAndCreateLock(BaseBoneDriver, BasisBoneTrackedRole.LeftHand, BasisBoneTrackedRole.LeftLittleProximal, BasisTargetController.TargetDirectional, 40, BasisClampData.None, 0, 7, false, 4);
            SetAndCreateLock(BaseBoneDriver, BasisBoneTrackedRole.LeftLittleProximal, BasisBoneTrackedRole.LeftLittleIntermediate, BasisTargetController.TargetDirectional, 40, BasisClampData.None, 0, 12, false, 4);
            SetAndCreateLock(BaseBoneDriver, BasisBoneTrackedRole.LeftLittleIntermediate, BasisBoneTrackedRole.LeftLittleDistal, BasisTargetController.TargetDirectional, 40, BasisClampData.None, 0, 12, false, 4);

            // Setting up locks for Right Hand
            SetAndCreateLock(BaseBoneDriver, BasisBoneTrackedRole.RightHand, BasisBoneTrackedRole.RightThumbProximal, BasisTargetController.TargetDirectional, 40, BasisClampData.None, 0, 7, false, 4);
            SetAndCreateLock(BaseBoneDriver, BasisBoneTrackedRole.RightThumbProximal, BasisBoneTrackedRole.RightThumbIntermediate, BasisTargetController.TargetDirectional, 40, BasisClampData.None, 0, 7, false, 4);
            SetAndCreateLock(BaseBoneDriver, BasisBoneTrackedRole.RightThumbIntermediate, BasisBoneTrackedRole.RightThumbDistal, BasisTargetController.TargetDirectional, 40, BasisClampData.None, 0, 7, false, 4);

            SetAndCreateLock(BaseBoneDriver, BasisBoneTrackedRole.RightHand, BasisBoneTrackedRole.RightIndexProximal, BasisTargetController.TargetDirectional, 40, BasisClampData.None, 0, 7, false, 4);
            SetAndCreateLock(BaseBoneDriver, BasisBoneTrackedRole.RightIndexProximal, BasisBoneTrackedRole.RightIndexIntermediate, BasisTargetController.TargetDirectional, 40, BasisClampData.None, 0, 7, false, 4);
            SetAndCreateLock(BaseBoneDriver, BasisBoneTrackedRole.RightIndexIntermediate, BasisBoneTrackedRole.RightIndexDistal, BasisTargetController.TargetDirectional, 40, BasisClampData.None, 0, 7, false, 4);

            SetAndCreateLock(BaseBoneDriver, BasisBoneTrackedRole.RightHand, BasisBoneTrackedRole.RightMiddleProximal, BasisTargetController.TargetDirectional, 40, BasisClampData.None, 0, 7, false, 4);
            SetAndCreateLock(BaseBoneDriver, BasisBoneTrackedRole.RightMiddleProximal, BasisBoneTrackedRole.RightMiddleIntermediate, BasisTargetController.TargetDirectional, 40, BasisClampData.None, 0, 7, false, 4);
            SetAndCreateLock(BaseBoneDriver, BasisBoneTrackedRole.RightMiddleIntermediate, BasisBoneTrackedRole.RightMiddleDistal, BasisTargetController.TargetDirectional, 40, BasisClampData.None, 0, 7, false, 4);

            SetAndCreateLock(BaseBoneDriver, BasisBoneTrackedRole.RightHand, BasisBoneTrackedRole.RightRingProximal, BasisTargetController.TargetDirectional, 40, BasisClampData.None, 0, 7, false, 4);
            SetAndCreateLock(BaseBoneDriver, BasisBoneTrackedRole.RightRingProximal, BasisBoneTrackedRole.RightRingIntermediate, BasisTargetController.TargetDirectional, 40, BasisClampData.None, 0, 7, false, 4);
            SetAndCreateLock(BaseBoneDriver, BasisBoneTrackedRole.RightRingIntermediate, BasisBoneTrackedRole.RightRingDistal, BasisTargetController.TargetDirectional, 40, BasisClampData.None, 0, 7, false, 4);

            SetAndCreateLock(BaseBoneDriver, BasisBoneTrackedRole.RightHand, BasisBoneTrackedRole.RightLittleProximal, BasisTargetController.TargetDirectional, 40, BasisClampData.None, 0, 7, false, 4);
            SetAndCreateLock(BaseBoneDriver, BasisBoneTrackedRole.RightLittleProximal, BasisBoneTrackedRole.RightLittleIntermediate, BasisTargetController.TargetDirectional, 40, BasisClampData.None, 0, 7, false, 4);
            SetAndCreateLock(BaseBoneDriver, BasisBoneTrackedRole.RightLittleIntermediate, BasisBoneTrackedRole.RightLittleDistal, BasisTargetController.TargetDirectional, 40, BasisClampData.None, 0, 7, false, 4);
        }
        public bool IsAble()
        {
            if (IsNull(LocalPlayer))
            {
                return false;
            }
            if (IsNull(LocalDriver))
            {
                return false;
            }
            if (IsNull(Player.Avatar))
            {
                return false;
            }
            if (IsNull(Player.Avatar.Animator))
            {
                return false;
            }
            return true;
        }
        public void SetBodySettings(BasisLocalBoneDriver driver)
        {
            GameObject HeadRig = CreateRig("chest, neck, head", true, out RigHeadRig, out RigHeadLayer);
            CreateTwoBone(driver, HeadRig, References.chest, References.neck, References.head, BasisBoneTrackedRole.Head, BasisBoneTrackedRole.Neck, true, out HeadTwoBoneIK, false, false);
            if (driver.FindBone(out BasisBoneControl Control, BasisBoneTrackedRole.Head))
            {
                WriteUpEvents(Control, RigHeadLayer);
            }

            GameObject RightShoulder = CreateRig("Upperchest, RightShoulder, RightUpperArm", true, out RightShoulderRig, out RightShoulderLayer);
            CreateTwoBone(driver, RightShoulder, References.chest, References.RightShoulder, References.RightUpperArm, BasisBoneTrackedRole.RightUpperArm, BasisBoneTrackedRole.RightShoulder, true, out RightShoulderTwoBoneIK, false, false);
            if (driver.FindBone(out Control, BasisBoneTrackedRole.RightShoulder))
            {
                WriteUpEvents(Control, RightShoulderLayer);
            }

            GameObject LeftShoulder = CreateRig("UpperChest, leftShoulder, leftUpperArm", true, out LeftShoulderRig, out LeftShoulderLayer);
            CreateTwoBone(driver, LeftShoulder, References.chest, References.leftShoulder, References.leftUpperArm, BasisBoneTrackedRole.LeftUpperArm, BasisBoneTrackedRole.LeftShoulder, true, out LeftShoulderTwoBoneIK, false, false);
            if (driver.FindBone(out Control, BasisBoneTrackedRole.LeftShoulder))
            {
                WriteUpEvents(Control, LeftShoulderLayer);
            }

            GameObject Body = CreateRig("Spine", true, out ChestSpineRig, out ChestSpineLayer);
            CreateTwoBone(driver, Body, References.spine, null, null, BasisBoneTrackedRole.Spine, BasisBoneTrackedRole.Neck, true, out UpperChestTwoBoneIK, false, false);
            if (driver.FindBone(out Control, BasisBoneTrackedRole.Chest))
            {
                WriteUpEvents(Control, ChestSpineLayer);
            }
            LeftHand(driver);
            RightHand(driver);
            LeftFoot(driver);
            RightFoot(driver);
            LeftToe(driver);
            RightToe(driver);
        }
        public void LeftHand(BasisLocalBoneDriver driver)
        {
            GameObject Hands = CreateRig("leftUpperArm, leftLowerArm, leftHand", false, out LeftHandRig, out LeftHandLayer);
            if (driver.FindBone(out BasisBoneControl Control, BasisBoneTrackedRole.LeftHand))
            {
                WriteUpEvents(Control, LeftHandLayer);
            }
            CreateTwoBone(driver, Hands, References.leftUpperArm, References.leftLowerArm, References.leftHand, BasisBoneTrackedRole.LeftHand, BasisBoneTrackedRole.LeftLowerArm, true, out LeftHandTwoBoneIK, false, false);
        }
        public void RightHand(BasisLocalBoneDriver driver)
        {
            GameObject Hands = CreateRig("RightUpperArm, RightLowerArm, rightHand", false, out RightHandRig, out RightHandLayer);
            if (driver.FindBone(out BasisBoneControl Control, BasisBoneTrackedRole.RightHand))
            {
                WriteUpEvents(Control, RightHandLayer);
            }
            CreateTwoBone(driver, Hands, References.RightUpperArm, References.RightLowerArm, References.rightHand, BasisBoneTrackedRole.RightHand, BasisBoneTrackedRole.RightLowerArm, true, out RightHandTwoBoneIK, false, false);
        }
        public void LeftFoot(BasisLocalBoneDriver driver)
        {
            GameObject feet = CreateRig("LeftUpperLeg, LeftLowerLeg, leftFoot", false, out LeftFootRig, out LeftFootLayer);
            if (driver.FindBone(out BasisBoneControl Control, BasisBoneTrackedRole.LeftFoot))
            {
                WriteUpEvents(Control, LeftFootLayer);
            }
            CreateTwoBone(driver, feet, References.LeftUpperLeg, References.LeftLowerLeg, References.leftFoot, BasisBoneTrackedRole.LeftFoot, BasisBoneTrackedRole.LeftLowerLeg, true, out LeftFootTwoBoneIK, false, false);
        }
        public void RightFoot(BasisLocalBoneDriver driver)
        {
            GameObject feet = CreateRig("RightUpperLeg, RightLowerLeg, rightFoot", false, out RightFootRig, out RightFootLayer);
            if (driver.FindBone(out BasisBoneControl Control, BasisBoneTrackedRole.RightFoot))
            {
                WriteUpEvents(Control, RightFootLayer);
            }
            CreateTwoBone(driver, feet, References.RightUpperLeg, References.RightLowerLeg, References.rightFoot, BasisBoneTrackedRole.RightFoot, BasisBoneTrackedRole.RightLowerLeg, true, out RightFootTwoBoneIK, false, false);
        }
        public void LeftToe(BasisLocalBoneDriver driver)
        {
            GameObject LeftToe = CreateRig("LeftToe", false, out LeftToeRig, out LeftToeLayer);
            if (driver.FindBone(out BasisBoneControl Control, BasisBoneTrackedRole.LeftToes))
            {
                WriteUpEvents(Control, LeftToeLayer);
            }
            Damp(driver, LeftToe, References.leftToes, BasisBoneTrackedRole.LeftToes, 0, 0);
        }
        public void RightToe(BasisLocalBoneDriver driver)
        {
            GameObject RightToe = CreateRig("RightToe", false, out RightToeRig, out RightToeLayer);
            if (driver.FindBone(out BasisBoneControl Control, BasisBoneTrackedRole.RightToes))
            {
                WriteUpEvents(Control, RightToeLayer);
            }
            Damp(driver, RightToe, References.rightToes, BasisBoneTrackedRole.RightToes, 0, 0);
        }
        public void CalibrateRoles()
        {
            for (int Index = 0; Index < BasisLocalPlayer.Instance.LocalBoneDriver.AllBoneControls.Count; Index++)
            {
                BasisBoneTrackedRole role = BasisLocalPlayer.Instance.LocalBoneDriver.AllBoneRoles[Index];
                BasisBoneControl BoneControl = BasisLocalPlayer.Instance.LocalBoneDriver.AllBoneControls[Index];
                if (BoneControl.HasRigLayer == BasisHasRigLayer.HasRigLayer)
                {
                    ApplyHint(role, 1);
                }
                else
                {
                    ApplyHint(role, 0);
                }
            }
        }
        public void ApplyHint(BasisBoneTrackedRole RoleWithHint, int weight)
        {
            switch (RoleWithHint)
            {
                case BasisBoneTrackedRole.Neck:
                    // Debug.Log("Setting Hint For " + RoleWithHint + " with weight " + weight);
                    HeadTwoBoneIK.data.hintWeight = weight;
                    break;

                case BasisBoneTrackedRole.RightLowerLeg:
                    // Debug.Log("Setting Hint For " + RoleWithHint + " with weight " + weight);
                    RightFootTwoBoneIK.data.hintWeight = weight;
                    break;

                case BasisBoneTrackedRole.LeftLowerLeg:
                    // Debug.Log("Setting Hint For " + RoleWithHint + " with weight " + weight);
                    LeftFootTwoBoneIK.data.hintWeight = weight;
                    break;

                case BasisBoneTrackedRole.RightUpperArm:
                    // Debug.Log("Setting Hint For " + RoleWithHint + " with weight " + weight);
                    RightHandTwoBoneIK.data.hintWeight = weight;
                    break;

                case BasisBoneTrackedRole.LeftUpperArm:
                    // Debug.Log("Setting Hint For " + RoleWithHint + " with weight " + weight);
                    LeftHandTwoBoneIK.data.hintWeight = weight;
                    break;

                case BasisBoneTrackedRole.LeftShoulder:
                    // Debug.Log("Setting Hint For " + RoleWithHint + " with weight " + weight);
                    LeftShoulderTwoBoneIK.data.hintWeight = weight;
                    break;

                case BasisBoneTrackedRole.RightShoulder:
                    // Debug.Log("Setting Hint For " + RoleWithHint + " with weight " + weight);
                    RightShoulderTwoBoneIK.data.hintWeight = weight;
                    break;

                default:
                    // Optional: Handle cases where RoleWithHint does not match any of the expected roles
                    // Debug.Log("Unknown role: " + RoleWithHint);
                    break;
            }
        }
        /// <summary>
        /// this gets cleared on a calibration
        /// </summary>
        /// <param name="Control"></param>
        /// <param name="Layer"></param>
        public void WriteUpEvents(BasisBoneControl Control, RigLayer Layer)
        {
            Control.OnHasRigChanged.AddListener(delegate { UpdateLayerActiveState(Control, Layer); });
            Control.HasEvents = true;
            // Set the initial state
            UpdateLayerActiveState(Control, Layer);
        }
        // Define a method to update the active state of the Layer
        void UpdateLayerActiveState(BasisBoneControl Control, RigLayer Layer)
        {
            // Debug.Log("setting Layer State to " + Control.HasRigLayer == BasisHasRigLayer.HasRigLayer + " for " + Control.Name);
            Layer.active = Control.HasRigLayer == BasisHasRigLayer.HasRigLayer;
        }
        public GameObject CreateRig(string Role, bool Enabled, out Rig Rig, out RigLayer RigLayer)
        {
            GameObject RigGameobject = CreateAndSetParent(Player.Avatar.Animator.transform, "Rig " + Role);
            Rig = BasisHelpers.GetOrAddComponent<Rig>(RigGameobject);
            Rigs.Add(Rig);
            RigLayer = new RigLayer(Rig, Enabled);
            Builder.layers.Add(RigLayer);
            return RigGameobject;
        }
    }
}