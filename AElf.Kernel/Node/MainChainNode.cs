using System.Threading.Tasks;
using AElf.Kernel.Managers;
using AElf.Kernel.Node.RPC;
using AElf.Kernel.TxMemPool;

namespace AElf.Kernel.Node
{
    public class MainChainNode : IAElfNode
    {
        private readonly ITxPoolService _poolService;
        private readonly ITransactionManager _transactionManager;
        private readonly IRpcServer _rpcServer;
        
        public MainChainNode(ITxPoolService poolService, ITransactionManager txManager, IRpcServer rpcServer)
        {
            _poolService = poolService;
            _transactionManager = txManager;
            _rpcServer = rpcServer;
        }

        public void Start()
        {
            _poolService.Start();
            _rpcServer.Start();
            
            // todo : avoid circular dependency
            _rpcServer.SetCommandContext(this);
        }

        public async Task<ITransaction> GetTransaction(Hash txId)
        {
            return await _transactionManager.GetTransaction(txId);
        }

        public async Task<IHash> InsertTransaction(ITransaction tx)
        {
            return await _transactionManager.AddTransactionAsync(tx);
        }
    }
}