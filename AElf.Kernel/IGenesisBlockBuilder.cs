using AElf.Kernel.KernelAccount;

namespace AElf.Kernel
{
    public interface IGenesisBlockBuilder
    {
        IGenesisBlock Build(IHash<IChain> chainId,ISmartContractZero smartContractZero);
    }
}