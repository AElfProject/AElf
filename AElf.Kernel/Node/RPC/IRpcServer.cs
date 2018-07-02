namespace AElf.Kernel.Node.RPC
{
    public interface IRpcServer
    {
        bool Start(int rpcPort);
        void SetCommandContext(MainChainNode node);
    }
}