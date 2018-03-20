using AElf.Kernel.KernelAccount;

namespace AElf.Kernel
{
    public class GenesisBlockBuilder 
    {
        public GenesisBlock Block { get; set; }
        
        public Transaction Tx { get; set; }


        public GenesisBlockBuilder Build(IHash<IChain> chainId, ISmartContractZero smartContractZero)
        {
            var block = new GenesisBlock()
            {

            };
            var tx = new Transaction
            {
                From = new Account(Hash<IAccount>.Zero),
                To = new Account(Hash<IAccount>.Zero),
                IncrementId = 0,
                MethodName = nameof(ISmartContractZero.RegisterSmartContract),
                Params = new object[]
                {
                    new SmartContractRegistration()
                    {
                        Category = 0,
                        Hash = smartContractZero.GetHash(),
                    }
                }
                
            };
            block.AddTransaction(tx.GetHash());

            Block = block;
            Tx = tx;

            return this;
        }
    }
}