namespace AElf.Kernel.SmartContract.Application
{
    public interface IChainContext<T> : IChainContext where T : IStateCache
    {
        new T StateCache { get; set; }
    }
}