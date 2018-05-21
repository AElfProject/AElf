namespace AElf.Kernel.Node.Network.Peers
{
    public interface IPeerDatabase
    {
        void Initialize();
        
        void AddPeer(string peerID);
        void RemovePeer(string peerId);
    }
}