namespace AElf.Kernel.Node.RPC
{
    public interface IRpcServer
    {
        bool Start(string host, int rpcPort);
        void SetCommandContext(MainChainNode node);
    }
}