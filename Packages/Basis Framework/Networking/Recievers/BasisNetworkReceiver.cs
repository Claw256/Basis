using Basis.Scripts.BasisSdk.Players;
using Basis.Scripts.Networking.NetworkedAvatar;
using Basis.Scripts.Networking.NetworkedPlayer;
using Basis.Scripts.Networking.Smoothing;
using BasisSerializer.OdinSerializer;
using System;
using UnityEngine;
using UnityEngine.AddressableAssets;
using static SerializableDarkRift;

namespace Basis.Scripts.Networking.Recievers
{
    [DefaultExecutionOrder(15001)]
    [System.Serializable]
    public partial class BasisNetworkReceiver : BasisNetworkSendBase
    {
        public Vector3 ScaleOffset;
        public Vector3 PlayerPosition;

        private float lerpTimeSpeedMovement = 0;
        private float lerpTimeSpeedRotation = 0;
        private float lerpTimeSpeedMuscles = 0;
        public float[] silentData;
        public BasisAvatarLerpDataSettings Settings;

        [SerializeField]
        public BasisAudioReceiver AudioReceiverModule = new BasisAudioReceiver();

        public BasisRemotePlayer RemotePlayer;
        public bool HasEvents = false;
        public override void Compute()
        {
            if (!IsAbleToUpdate())
                return;

            float deltaTime = Time.deltaTime;
            lerpTimeSpeedMovement = deltaTime * Settings.LerpSpeedMovement;
            lerpTimeSpeedRotation = deltaTime * Settings.LerpSpeedRotation;
            lerpTimeSpeedMuscles = deltaTime * Settings.LerpSpeedMuscles;

            BasisAvatarLerp.UpdateAvatar(ref Output, Target, AvatarJobs, lerpTimeSpeedMovement, lerpTimeSpeedRotation, lerpTimeSpeedMuscles, Settings.TeleportDistance);

            ApplyPoseData(NetworkedPlayer.Player.Avatar.Animator, Output, ref HumanPose);
            PoseHandler.SetHumanPose(ref HumanPose);

            RemotePlayer.RemoteBoneDriver.SimulateOnRender();
            RemotePlayer.UpdateTransform(RemotePlayer.MouthControl.OutgoingWorldData.position, RemotePlayer.MouthControl.OutgoingWorldData.rotation);
        }

        public void LateUpdate()
        {
            if (Ready)
            {
                Compute();
                AudioReceiverModule.LateUpdate();
            }
        }

        public bool IsAbleToUpdate()
        {
            return NetworkedPlayer != null && NetworkedPlayer.Player != null && NetworkedPlayer.Player.Avatar != null;
        }
        public void ApplyPoseData(Animator animator, BasisAvatarData output, ref HumanPose pose)
        {
            pose.bodyPosition = output.Vectors[1];
            pose.bodyRotation = output.Quaternions[0];
            if (pose.muscles == null || pose.muscles.Length != output.Muscles.Length)
            {
                pose.muscles = output.Muscles.ToArray();
            }
            else
            {
                output.Muscles.CopyTo(pose.muscles);
            }

            PlayerPosition = Output.Vectors[0];//world position
            animator.transform.localScale = Output.Vectors[2];//scale

            //we scale the position by the scale
            ScaleOffset = Output.Vectors[2] - Vector3.one;

            PlayerPosition.Scale(ScaleOffset);
           // animator.humanScale
            animator.transform.position = -PlayerPosition;
        }
        public void ReceiveNetworkAudio(AudioSegmentMessage audioSegment)
        {
            if (AudioReceiverModule.decoder != null)
            {
                AudioReceiverModule.decoder.OnEncoded(audioSegment.audioSegmentData.buffer);
            }
        }

        public void ReceiveSilentNetworkAudio(AudioSilentSegmentDataMessage audioSilentSegment)
        {
            if (AudioReceiverModule.decoder != null)
            {
                if (silentData == null || silentData.Length != AudioReceiverModule.SegmentSize)
                {
                    silentData = new float[AudioReceiverModule.SegmentSize];
                    Array.Fill(silentData, 0f);
                }
                AudioReceiverModule.OnDecoded(silentData);
            }
        }
        public void ReceiveNetworkAvatarData(ServerSideSyncPlayerMessage serverSideSyncPlayerMessage)
        {
            BasisNetworkAvatarDecompressor.DeCompress(this, serverSideSyncPlayerMessage);
        }
        public void ReceiveAvatarChangeRequest(ServerAvatarChangeMessage ServerAvatarChangeMessage)
        {
            BasisLoadableBundle BasisLoadableBundle = BasisBundleConversionNetwork.ConvertNetworkBytesToBasisLoadableBundle(ServerAvatarChangeMessage.clientAvatarChangeMessage.byteArray);

            RemotePlayer.CreateAvatar(ServerAvatarChangeMessage.clientAvatarChangeMessage.loadMode, BasisLoadableBundle);
        }
        public override async void Initialize(BasisNetworkedPlayer networkedPlayer)
        {
            if (!Ready)
            {
                InitalizeDataJobs();
                InitalizeAvatarStoredData(ref Target);
                InitalizeAvatarStoredData(ref Output);
                UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationHandle<BasisAvatarLerpDataSettings> handle = Addressables.LoadAssetAsync<BasisAvatarLerpDataSettings>(BasisAvatarLerp.Settings);
                await handle.Task;
                Settings = handle.Result;
                Ready = true;
                NetworkedPlayer = networkedPlayer;
                RemotePlayer = (BasisRemotePlayer)NetworkedPlayer.Player;
                AudioReceiverModule.OnEnable(networkedPlayer, gameObject);
                OnAvatarCalibration();
                if (HasEvents == false)
                {
                    RemotePlayer.RemoteAvatarDriver.CalibrationComplete += OnCalibration;
                    HasEvents = true;
                }
            }
        }
        public void OnDestroy()
        {
            Target.Vectors.Dispose();
            Target.Quaternions.Dispose();
            Target.Muscles.Dispose();

            Output.Vectors.Dispose();
            Output.Quaternions.Dispose();
            Output.Muscles.Dispose();

            if (HasEvents && RemotePlayer != null && RemotePlayer.RemoteAvatarDriver != null)
            {
                RemotePlayer.RemoteAvatarDriver.CalibrationComplete -= OnCalibration;
                HasEvents = false;
            }

            if (AudioReceiverModule != null)
            {
                AudioReceiverModule.OnDestroy();
            }
        }
        public void OnCalibration()
        {
            AudioReceiverModule.OnCalibration(NetworkedPlayer);
        }

        public override void DeInitialize()
        {
            AudioReceiverModule.OnDisable();
        }
    }
}