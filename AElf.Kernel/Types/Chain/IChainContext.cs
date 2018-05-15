using AElf.Kernel.KernelAccount;

namespace AElf.Kernel
{
    /// <summary>
    /// a running chain context
    /// </summary>
    public interface IChainContext
    {
        ISmartContractZero SmartContractZero { get; }
        Hash ChainId { get; }
    }
}