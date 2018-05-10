using AElf.Kernel.Managers;
using AElf.Kernel.TxMemPool;

namespace AElf.Kernel
{
    public class AElfNode
    {
        private ITxPoolService _poolService;
        private ITransactionManager _transactionManager;
        
        public AElfNode(ITxPoolService poolService, ITransactionManager txManager)
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