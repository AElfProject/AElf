using AElf.Kernel.Infrastructure;

namespace AElf.Kernel.Blockchain.Infrastructure
{
    public interface IBlockchainStore<T> : IKeyValueStore<T>
    {
    }
}