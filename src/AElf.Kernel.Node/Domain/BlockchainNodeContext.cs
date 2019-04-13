using AElf.Kernel.Node.Infrastructure;
using AElf.Kernel.TransactionPool.Infrastructure;

namespace AElf.Kernel.Node.Domain
{
    public class BlockchainNodeContext
    {
        public ITxHub TxHub { get; set; }
        
        public int ChainId { get; set; }
    }
}