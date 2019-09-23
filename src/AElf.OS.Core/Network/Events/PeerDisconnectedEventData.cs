namespace AElf.OS.Network.Events
{
    public class PeerDisconnectedEventData
    {
        public NodeInfo NodeInfo { get; }

        public PeerDisconnectedEventData(NodeInfo _nodeInfo)
        {
            NodeInfo = _nodeInfo;
        }
    }
}