using AElf.Kernel.KernelAccount;

namespace AElf.Kernel
{
    /// <summary>
    /// a running chain context
    /// </summary>
    public interface IChainContext
    {
        ISmartContractZero SmartContractZero { get; }
        ulong CurrentBlockHeight { get; }
        Hash ChainId { get; }
    }
}