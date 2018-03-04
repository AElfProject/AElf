using AElf.Kernel.KernelAccount;

namespace AElf.Kernel
{
    public interface IGenesisBlockBuilder
    {
        IGenesisBlock Build(IHash<IChain> chainId,ISmartContractZero smartContractZero);
    }

    public class GenesisBlockBuilder : IGenesisBlockBuilder
    {
        
        
        public IGenesisBlock Build(IHash<IChain> chainId, ISmartContractZero smartContractZero)
        {
            GenesisBlock block=new GenesisBlock();
            
            throw new System.NotImplementedException();
        }
    }
}