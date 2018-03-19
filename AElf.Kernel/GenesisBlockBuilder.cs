using AElf.Kernel.KernelAccount;

namespace AElf.Kernel
{
    public class GenesisBlockBuilder : IGenesisBlockBuilder
    {
        private IBlockManager _blockManager;

        public GenesisBlockBuilder(IBlockManager blockManager)
        {
            _blockManager = blockManager;
        }

        public IGenesisBlock Build(IHash<IChain> chainId, ISmartContractZero smartContractZero)
        {
            var block = new GenesisBlock()
            {

            };
            var tx = new Transaction
            {
                From = new Account(Hash<IAccount>.Zero),
                To = new Account(Hash<IAccount>.Zero),
                IncrementId = 0,
                MethodName = nameof(ISmartContractZero.RegisterSmartContract)
            };
            block.AddTransaction(tx.GetHash());
            
            
            throw new System.NotImplementedException();
        }
    }
}