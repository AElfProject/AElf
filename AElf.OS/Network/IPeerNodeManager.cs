namespace AElf.OS.Network
{
    public interface IPeerNodeManager
    {
        IPeerNode CreatePeerNode(IPeerNodeInfo info);
    }
    
}