using AElf.Kernel.TransactionPool.Application;

namespace AElf.Kernel.Node.Domain
{
    public class BlockchainNodeContext
    {
        public ITransactionPoolService TransactionPoolService { get; set; }
        
        public int ChainId { get; set; }
    }
}