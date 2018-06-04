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
    }
}