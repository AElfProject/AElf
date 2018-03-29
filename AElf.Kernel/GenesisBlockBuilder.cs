using AElf.Kernel.KernelAccount;

namespace AElf.Kernel
{
    public class GenesisBlockBuilder 
    {
        public GenesisBlock Block { get; set; }
        
        public Transaction Tx { get; set; }


        public GenesisBlockBuilder Build(ISmartContractZero smartContractZero)
        {
            var block = new GenesisBlock()
            {

            };
            var tx = new Transaction
            {
                IncrementId = 0,
                MethodName = nameof(ISmartContractZero.RegisterSmartContract),
                Params = new object[]
                {
                    new SmartContractRegistration()
                    {
                        Category = 0,
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