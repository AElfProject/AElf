using AElf.Kernel.KernelAccount;

namespace AElf.Kernel
{
    public class ChainContext : IChainContext
    {
        public ChainContext(ISmartContractZero smartContractZero, IHash chainId)
        {
            SmartContractZero = smartContractZero;
            ChainId = chainId;
        }

        public ISmartContractZero SmartContractZero { get; }
        public IHash ChainId { get; }
        
    }
}