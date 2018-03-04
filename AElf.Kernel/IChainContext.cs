using AElf.Kernel.KernelAccount;

namespace AElf.Kernel
{
    public interface IChainContext
    {
        ISmartContractZero SmartContractZero { get; }
        IHash<IChain> ChainId { get; }
    }
}