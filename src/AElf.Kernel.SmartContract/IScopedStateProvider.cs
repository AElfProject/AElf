using AElf.Types;

namespace AElf.Kernel.SmartContract
{
    public interface IScopedStateProvider : ICachedStateProvider
    {
        Address ContractAddress { get; }
    }
}