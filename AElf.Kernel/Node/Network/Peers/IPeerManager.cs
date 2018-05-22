namespace AElf.Kernel.Node.Network.Peers
{
    public interface IPeerManager
    {
        void Start();
        void AddPeer(Peer peer);
    }
}