using AElf.Kernel.KernelAccount;

namespace AElf.Kernel
{
    public class GenesisBlockBuilder 
    {
        public Block Block { get; set; }
        
        public Transaction Tx { get; set; }


        public GenesisBlockBuilder Build(ISmartContractZero smartContractZero)
        {
            var block = new Block()
            {

            };
            var tx = new Transaction
            {
                IncrementId = 0,
                MethodName = nameof(ISmartContractZero.RegisterSmartContract),
                
                
            };
            block.AddTransaction(tx.GetHash());

            Block = block;
            
            Tx = tx;

            return this;
        }
    }
}