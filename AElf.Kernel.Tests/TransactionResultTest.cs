using System.Threading.Tasks;
using AElf.Kernel.Managers;
using AElf.ChainController;
using AElf.ChainController.Rpc;
using Xunit;
using Xunit.Frameworks.Autofac;
using AElf.Common;
using AElf.Configuration;
using AElf.Miner.TxMemPool;
using NLog;

namespace AElf.Kernel.Tests
{
    [UseAutofacTestFramework]
    public class TransactionResultTest
    {
        private readonly ITransactionResultService _transactionResultService;
        private readonly ITransactionResultManager _transactionResultManager;

        public TransactionResultTest(ITxPoolConfig txPoolConfig, IChainService chainService,
            ITransactionManager transactionManager, ITransactionResultManager transactionResultManager, ILogger logger)
        {
            NodeConfig.Instance.ChainId = Hash.Generate().DumpHex();
            NodeConfig.Instance.NodeAccount = Address.Generate().DumpHex();
            _transactionResultManager = transactionResultManager;
            _transactionResultService = new TransactionResultService(
                new TxPool(logger, new TxValidator(txPoolConfig, chainService, logger),
                    new NewTxHub(transactionManager, chainService)), transactionResultManager);
        }

        private TransactionResult CreateResult(Hash txId, Status status)
        {
            return new TransactionResult
            {
                TransactionId = txId,
                Status = status
            };
        }

        
        [Fact]
        public async Task TxResultStorage()
        {
            var txId = Hash.Generate();
            var res = CreateResult(txId, Status.Mined);
            await _transactionResultManager.AddTransactionResultAsync(res);
            var r = await _transactionResultManager.GetTransactionResultAsync(txId);
            Assert.Equal(res, r);
        }
        
        [Fact]
        public async Task AddTxResult()
        {
            var txId = Hash.Generate();
            var res = new TransactionResult
            {
                TransactionId = txId,
                Status = Status.Mined
            };
            await _transactionResultService.AddResultAsync(res);
        }
        
        [Fact]
        public async Task GetTxResultNotExisted()
        {
            var txId = Hash.Generate();
            var res = await _transactionResultService.GetResultAsync(txId);
            Assert.Equal(txId, res.TransactionId);
            Assert.Equal(res.Status, Status.NotExisted);
        }

        [Fact]
        public async Task GetTxResultInStorage()
        {
            var txId = Hash.Generate();
            var res = CreateResult(txId, Status.Mined);
            await _transactionResultManager.AddTransactionResultAsync(res);

            var r = await _transactionResultService.GetResultAsync(txId);
            Assert.Equal(res, r);
        }

    }
}