using AElf.Kernel;

namespace AElf.Miner.Rpc.Server
{
    public interface IServerImpl
    {
        void Init(Hash chanId);
    }
}