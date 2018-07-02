using System.Net;
using Google.Protobuf;

namespace AElf.Network.Data
{
    public static class NetRequestFactory
    {
        public static AElfPacketData CreateMissingPeersReq(int peersMissing)
        {
            var reqPeerListData = new ReqPeerListData { NumPeers = peersMissing };

            var request = new AElfPacketData
            {
                MsgType = (int) MessageTypes.RequestPeers,
                Length = 1,
                Payload = reqPeerListData.ToByteString()
            };

            return request;
        }

        public static AElfPacketData CreateRequest(MessageTypes messageType, byte[] payload, int? messageId)
        {
            AElfPacketData packetData = new AElfPacketData
            {
                Id = messageId ?? 0,
                MsgType = (int)messageType,
                Length = payload.Length,
                Payload = ByteString.CopyFrom(payload)
            };

            return packetData;
        }
    }
}