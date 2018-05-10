using AElf.Kernel.Managers;
using AElf.Kernel.TxMemPool;

namespace AElf.Kernel.Node
{
    public class MainChainNode : IAElfNode
    {
        private readonly ITxPoolService _poolService;
        private readonly ITransactionManager _transactionManager;
        
        public MainChainNode(ITxPoolService poolService, ITransactionManager txManager)
        {
            _poolService = poolService;
            _transactionManager = txManager;
        }

        public void Start()
        {
            _poolService.Start();
        }
    }
}