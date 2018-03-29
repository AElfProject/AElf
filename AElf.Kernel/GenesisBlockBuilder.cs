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
                Header = new BlockHeader(Hash.Zero),
                Body = new BlockBody()
            };
            var tx = new Transaction
            {
                From = Hash.Zero,
                To = Hash.Zero,
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