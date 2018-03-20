using AElf.Kernel.KernelAccount;

namespace AElf.Kernel
{
    public class ChainContext : IChainContext
    {
        public ChainContext(ISmartContractZero smartContractZero, IHash<IChain> chainId)
        {
            SmartContractZero = smartContractZero;
            ChainId = chainId;
        }

        public ISmartContractZero SmartContractZero { get; }
        public IHash<IChain> ChainId { get; }
        
    }
}