using AElf.Network.Connection;
using Google.Protobuf;

namespace AElf.Network.Data
{
    public static class NetRequestFactory
    {
        public static Message CreateMissingPeersReq(int peersMissing)
        {
            var reqPeerListData = new ReqPeerListData { NumPeers = peersMissing };
            var payload = reqPeerListData.ToByteString().ToByteArray();

            var request = new Message
            {
                Type = (int) MessageType.RequestPeers,
                Length = payload.Length,
                Payload = payload
            };

            return request;
        }

        public static Message CreateMessage(AElfProtocolType messageType, byte[] payload)
        {
            Message packetData = new Message
            {
                Type = (int)messageType,
                Length = payload.Length,
                Payload = payload
            };

            return packetData;
        }
    }
}