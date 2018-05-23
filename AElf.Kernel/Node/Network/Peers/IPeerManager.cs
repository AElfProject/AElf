namespace AElf.Kernel.Node.Network.Peers
{
    public interface IPeerManager
    {
        void Start();
        void AddPeer(Peer peer);
        
        void SetCommandContext(MainChainNode node);
    }
}