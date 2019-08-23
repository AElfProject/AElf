using AElf.Kernel;

namespace AElf.OS.Network.Events
{
    public class PeerConnectedEventData
    {
        public BlockHeader BestChainHead { get; }
        public NodeInfo NodeInfo { get; }
        public PeerConnectedEventData(NodeInfo nodeInfo, BlockHeader bestChainHead)
        {
            BestChainHead = bestChainHead;
            NodeInfo = nodeInfo;
        }
    }
}