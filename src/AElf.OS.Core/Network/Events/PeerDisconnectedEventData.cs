namespace AElf.OS.Network.Events
{
    public class PeerDisconnectedEventData
    {
        public NodeInfo NodeInfo { get; }
        public bool IsInbound { get; }

        public PeerDisconnectedEventData(NodeInfo nodeInfo, bool isInbound)
        {
            NodeInfo = nodeInfo;
            IsInbound = isInbound;
        }
    }
}