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
            // todo 
            return await Task.Factory.StartNew(() =>
            {

                var tx = new Transaction()
                {
                    From = new Hash(new byte[] {0x01, 0x02}),
                    To = new Hash(new byte[] {0x01, 0x02})
                };

                return tx;
            });
            
            
            //return await _transactionManager.GetTransaction(txId);
        }
    }
}