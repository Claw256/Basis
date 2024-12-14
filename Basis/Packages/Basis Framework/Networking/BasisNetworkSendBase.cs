using Basis.Network.Core;
using Basis.Scripts.Networking.Compression;
using Basis.Scripts.Networking.NetworkedPlayer;
using LiteNetLib;
using LiteNetLib.Utils;
using System.Threading;
using UnityEngine;
using static BasisNetworkPrimitiveCompression;
using static SerializableBasis;

namespace Basis.Scripts.Networking.NetworkedAvatar
{
    /// <summary>
    /// the goal of this script is to be the glue of consistent data between remote and local
    /// </summary>
    public abstract class BasisNetworkSendBase : MonoBehaviour
    {
        public bool Ready;
        public BasisNetworkedPlayer NetworkedPlayer;
        private readonly object _lock = new object(); // Lock object for thread-safety
        private bool _hasReasonToSendAudio;
        public bool HasReasonToSendAudio
        {
            get
            {
                lock (_lock)
                {
                    return _hasReasonToSendAudio;
                }
            }
            set
            {
                lock (_lock)
                {
                    _hasReasonToSendAudio = value;
                }
            }
        }
        public static BasisRangedUshortFloatData RotationCompression = new BasisRangedUshortFloatData(-1f, 1f, 0.001f);
        [SerializeField]
        public HumanPose HumanPose = new HumanPose();
        [SerializeField]
        public PlayerIdMessage NetworkNetID = new PlayerIdMessage();
        [SerializeField]
        public HumanPoseHandler PoseHandler;
        [SerializeField]
        public static BasisRangedUshortFloatData PositionRanged = new BasisRangedUshortFloatData(-BasisNetworkConstants.MaxPosition, BasisNetworkConstants.MaxPosition, BasisNetworkConstants.PositionPrecision);
        [SerializeField]
        public static BasisRangedUshortFloatData ScaleRanged = new BasisRangedUshortFloatData(BasisNetworkConstants.MinimumScale, BasisNetworkConstants.MaximumScale, BasisNetworkConstants.ScalePrecision);
        public const int SizeAfterGap = 95 - SecondBuffer;
        public const int FirstBuffer = 15;
        public const int SecondBuffer = 21;
        public abstract void Initialize(BasisNetworkedPlayer NetworkedPlayer);
        public abstract void DeInitialize();
        public void OnAvatarCalibration()
        {
            if (BasisNetworkManagement.MainThreadContext == null)
            {
                Debug.LogError("Main thread context is not set. Ensure this script is started on the main thread.");
                return;
            }

            // Post the task to the main thread
            BasisNetworkManagement.MainThreadContext.Post(_ =>
            {
                if (NetworkedPlayer != null && NetworkedPlayer.Player != null && NetworkedPlayer.Player.Avatar != null)
                {

                    ComputeHumanPose();
                    if (!NetworkedPlayer.Player.Avatar.HasSendEvent)
                    {
                        NetworkedPlayer.Player.Avatar.OnNetworkMessageSend += OnNetworkMessageSend;
                        NetworkedPlayer.Player.Avatar.HasSendEvent = true;
                    }

                    NetworkedPlayer.Player.Avatar.LinkedPlayerID = NetworkedPlayer.NetId;
                    NetworkedPlayer.Player.Avatar.OnAvatarNetworkReady?.Invoke();
                }
            }, null);
        }
        public void ComputeHumanPose()
        {
            if (NetworkedPlayer != null && NetworkedPlayer.Player != null && NetworkedPlayer.Player.Avatar != null)
            {
                PoseHandler = new HumanPoseHandler(
                NetworkedPlayer.Player.Avatar.Animator.avatar,
                NetworkedPlayer.Player.Avatar.transform
            );
                PoseHandler.GetHumanPose(ref HumanPose);
            }
        }
        private void OnNetworkMessageSend(byte MessageIndex, byte[] buffer = null, DeliveryMethod DeliveryMethod = DeliveryMethod.Sequenced, ushort[] Recipients = null)
        {
            // Check if Recipients or buffer arrays are valid or not
            if (Recipients != null && Recipients.Length == 0) Recipients = null;

            if (buffer != null && buffer.Length == 0) buffer = null;

            ushort NetId = NetworkedPlayer.NetId;
            NetDataWriter netDataWriter = new NetDataWriter();
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
                    netDataWriter.Put(BasisNetworkTag.AvatarGenericMessage_NoRecipients_NoPayload);
                    AvatarDataMessage_NoRecipients_NoPayload.Serialize(netDataWriter);
                    // No recipients and no payload
                    WriteAndSendMessage(netDataWriter, DeliveryMethod);
                }
                else
                {
                    AvatarDataMessage_NoRecipients AvatarDataMessage_NoRecipients = new AvatarDataMessage_NoRecipients
                    {
                        playerIdMessage = new PlayerIdMessage() { playerID = NetId },
                        messageIndex = MessageIndex,
                        payload = buffer
                    };
                    netDataWriter.Put(BasisNetworkTag.AvatarGenericMessage_NoRecipients);
                    AvatarDataMessage_NoRecipients.Serialize(netDataWriter);
                    // No recipients but has payload
                    WriteAndSendMessage(netDataWriter, DeliveryMethod);
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
                    netDataWriter.Put(BasisNetworkTag.AvatarGenericMessage_Recipients_NoPayload);
                    AvatarDataMessage.Serialize(netDataWriter);
                    WriteAndSendMessage(netDataWriter, DeliveryMethod);
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
                    netDataWriter.Put(BasisNetworkTag.AvatarGenericMessage);
                    AvatarDataMessage.Serialize(netDataWriter);
                    // Recipients present, payload may or may not be present
                    WriteAndSendMessage(netDataWriter, DeliveryMethod);
                }
            }
        }

        // Helper method to avoid code duplication
        private void WriteAndSendMessage(NetDataWriter writer, DeliveryMethod deliveryMethod)
        {
            BasisNetworkManagement.LocalPlayerPeer.Send(writer, BasisNetworkCommons.AvatarChannel, deliveryMethod);
        }

    }
}