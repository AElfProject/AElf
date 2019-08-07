using AElf.Kernel;
using AElf.Types;

namespace AElf.OS.Network.Events
{
    public class PeerConnectedEventData
    {
        public BlockHeader BestChainHead { get; }
        public NodeInfo NodeInfo { get; }
        public Hash BestChainHash { get; }
        public long BestChainHeight { get; }

        public PeerConnectedEventData(NodeInfo nodeInfo, Hash bestChainHash, long bestChainHeight)
        {
            NodeInfo = nodeInfo;
            BestChainHash = bestChainHash;
            bestChainHeight = bestChainHeight;
        }
    }
}