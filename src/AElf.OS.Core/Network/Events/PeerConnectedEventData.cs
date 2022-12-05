using AElf.Types;

namespace AElf.OS.Network.Events;

public class PeerConnectedEventData
{
    public PeerConnectedEventData(NodeInfo nodeInfo, Hash bestChainHash, long bestChainHeight)
    {
        NodeInfo = nodeInfo;
        BestChainHash = bestChainHash;
        BestChainHeight = bestChainHeight;
    }

    public NodeInfo NodeInfo { get; }
    public Hash BestChainHash { get; }
    public long BestChainHeight { get; }
}