using Basis.Scripts.Networking.Compression;
using Basis.Scripts.Networking.NetworkedPlayer;
using Basis.Scripts.Tests;
using DarkRift.Server.Plugins.Commands;
using DarkRift;
using UnityEngine;
using static SerializableDarkRift;
using Unity.Mathematics;
using System.Collections.Generic;

namespace Basis.Scripts.Networking.NetworkedAvatar
{
    /// <summary>
    /// the goal of this script is to be the glue of consistent data between remote and local
    /// </summary>
    public abstract partial class BasisNetworkSendBase : MonoBehaviour
    {
        public bool Ready;
        public BasisNetworkedPlayer NetworkedPlayer;
        public static BasisRangedUshortFloatData RotationCompressor = new BasisRangedUshortFloatData(-1f, 1f, 0.001f);
        [System.Serializable]
        public struct AvatarBuffer
        {
            public quaternion rotation;
            public Vector3 Scale;
            public Vector3 Position;
            public float[] Muscles;
            public double timestamp;
        }
        [Header("Interpolation Settings")]
        public double delayTime = 0.1f; // How far behind real-time we want to stay, hopefully double is good.
        [SerializeField]
        public List<AvatarBuffer> AvatarDataBuffer = new List<AvatarBuffer>();
        public AvatarBuffer LastAvatarBuffer;
        public HumanPose HumanPose = new HumanPose();
        public LocalAvatarSyncMessage LASM = new LocalAvatarSyncMessage();
        public PlayerIdMessage NetworkNetID = new PlayerIdMessage();
        public BasisDataJobs AvatarJobs;
        public HumanPoseHandler PoseHandler;
        [SerializeField]
        public BasisRangedUshortFloatData PositionRanged;
        [SerializeField]
        public BasisRangedUshortFloatData ScaleRanged;
        public double TimeAsDoubleWhenLastSync;
        public float LastAvatarDelta = 0.1f;
        protected BasisNetworkSendBase()
        {
            LASM = new LocalAvatarSyncMessage()
            {
                array = new byte[212],
            };
            PositionRanged = new BasisRangedUshortFloatData(-BasisNetworkConstants.MaxPosition, BasisNetworkConstants.MaxPosition, BasisNetworkConstants.PositionPrecision);
            ScaleRanged = new BasisRangedUshortFloatData(BasisNetworkConstants.MinimumScale, BasisNetworkConstants.MaximumScale, BasisNetworkConstants.ScalePrecision);
        }
        public static void InitalizeDataJobs(ref BasisDataJobs BasisDataJobs)
        {
            BasisDataJobs.AvatarJob = new UpdateAvatarRotationJob();
            BasisDataJobs.muscleJob = new UpdateAvatarMusclesJob();
        }
        public abstract void Compute();
        public abstract void Initialize(BasisNetworkedPlayer NetworkedPlayer);
        public abstract void DeInitialize();
        public void OnAvatarCalibration()
        {
            if (NetworkedPlayer != null && NetworkedPlayer.Player != null && NetworkedPlayer.Player.Avatar != null)
            {
                PoseHandler = new HumanPoseHandler(NetworkedPlayer.Player.Avatar.Animator.avatar, NetworkedPlayer.Player.Avatar.transform.transform);
                if (NetworkedPlayer.Player.Avatar.HasSendEvent == false)
                {
                    NetworkedPlayer.Player.Avatar.OnNetworkMessageSend += OnNetworkMessageSend;
                    NetworkedPlayer.Player.Avatar.HasSendEvent = true;
                }
                NetworkedPlayer.Player.Avatar.LinkedPlayerID = NetworkedPlayer.NetId;
                NetworkedPlayer.Player.Avatar.OnAvatarNetworkReady?.Invoke();
            }
        }
        private void OnNetworkMessageSend(byte MessageIndex, byte[] buffer = null, DeliveryMethod DeliveryMethod = DeliveryMethod.Sequenced, ushort[] Recipients = null)
        {
            // Check if Recipients or buffer arrays are valid or not
            if (Recipients != null && Recipients.Length == 0) Recipients = null;

            if (buffer != null && buffer.Length == 0) buffer = null;

            ushort NetId = NetworkedPlayer.NetId;

            using (DarkRiftWriter writer = DarkRiftWriter.Create())
            {
                // Handle cases based on presence of Recipients and buffer
                if (Recipients == null)
                {
                    if (buffer == null)
                    {
                        AvatarDataMessage_NoRecipients_NoPayload AvatarDataMessage_NoRecipients_NoPayload = new AvatarDataMessage_NoRecipients_NoPayload
                        {
                            playerIdMessage = new PlayerIdMessage() { playerID = NetId },
                            messageIndex = MessageIndex
                        };
                        writer.Write(AvatarDataMessage_NoRecipients_NoPayload);
                        // No recipients and no payload
                        WriteAndSendMessage(BasisTags.AvatarGenericMessage_NoRecipients_NoPayload, writer, DeliveryMethod);
                    }
                    else
                    {
                        AvatarDataMessage_NoRecipients AvatarDataMessage_NoRecipients = new AvatarDataMessage_NoRecipients
                        {
                            playerIdMessage = new PlayerIdMessage() { playerID = NetId },
                            messageIndex = MessageIndex,
                            payload = buffer
                        };

                        writer.Write(AvatarDataMessage_NoRecipients);
                        // No recipients but has payload
                        WriteAndSendMessage(BasisTags.AvatarGenericMessage_NoRecipients, writer, DeliveryMethod);
                    }
                }
                else
                {
                    if (buffer == null)
                    {
                        AvatarDataMessage_Recipients_NoPayload AvatarDataMessage = new AvatarDataMessage_Recipients_NoPayload();
                        AvatarDataMessage.playerIdMessage = new PlayerIdMessage() { playerID = NetId };
                        AvatarDataMessage.messageIndex = MessageIndex;
                        AvatarDataMessage.recipients = Recipients;
                        // Recipients present, payload may or may not be present
                        writer.Write(AvatarDataMessage);
                        WriteAndSendMessage(BasisTags.AvatarGenericMessage_Recipients_NoPayload, writer, DeliveryMethod);
                    }
                    else
                    {
                        AvatarDataMessage AvatarDataMessage = new AvatarDataMessage
                        {
                            playerIdMessage = new PlayerIdMessage() { playerID = NetId },
                            messageIndex = MessageIndex,
                            payload = buffer,
                            recipients = Recipients
                        };
                        // Recipients present, payload may or may not be present
                        writer.Write(AvatarDataMessage);
                        WriteAndSendMessage(BasisTags.AvatarGenericMessage, writer, DeliveryMethod);
                    }
                }
            }
        }

        // Helper method to avoid code duplication
        private void WriteAndSendMessage(ushort tag, DarkRiftWriter writer, DeliveryMethod deliveryMethod)
        {
            using (var msg = Message.Create(tag, writer))
            {
                BasisNetworkManagement.Instance.Client.SendMessage(msg, BasisNetworking.AvatarChannel, deliveryMethod);
            }
        }
    }
}