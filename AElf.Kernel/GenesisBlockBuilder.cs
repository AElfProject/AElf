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

        public IGenesisBlock Build(ISmartContractZero smartContractZero, SmartContractRegistration smartContractRegistration)
        {
            
            var tx = new Transaction
            {
                From = new Account(Hash<IAccount>.Zero),
                To = new Account(Hash<IAccount>.Zero),
                IncrementId = 0,
                Params = new object[]{smartContractRegistration},
                MethodName = nameof(ISmartContractZero.RegisterSmartContract)
            };
            
            var block = new GenesisBlock(tx);
            block.AddTransaction(tx.GetHash());

            _blockManager.AddBlockAsync(block);
            return block;
        }
    }
}