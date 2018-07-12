using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AElf.Cryptography.ECDSA;
using AElf.ChainController;
using AElf.SmartContract;
using AElf.Kernel.Managers;
using AElf.Kernel.TxMemPool;
using Google.Protobuf;
using NLog;
using Xunit;
using Xunit.Frameworks.Autofac;

namespace AElf.Kernel.Tests.TxMemPool
{
    [UseAutofacTestFramework]
    public class TxPoolServiceTest
    {
        private readonly IAccountContextService _accountContextService;
        private readonly ILogger _logger;
        private readonly ITransactionManager _transactionManager;
        private readonly ITransactionResultManager _transactionResultManager;
        private readonly IWorldStateDictator _worldStateDictator;
        public TxPoolServiceTest(IAccountContextService accountContextService, ILogger logger,
            ITransactionManager transactionManager, ITransactionResultManager transactionResultManager, IWorldStateDictator worldStateDictator)
        {
            _accountContextService = accountContextService;
            _logger = logger;
            _transactionManager = transactionManager;
            _transactionResultManager = transactionResultManager;
            _worldStateDictator = worldStateDictator;
        }

        private TxPool GetPool()
        {
            var config = TxPoolConfig.Default;
            _worldStateDictator.SetChainId(config.ChainId);
            return new TxPool(config, _logger);
        }

        [Fact]
        public async Task AddTxTest()
        {
            var pool = GetPool();

            var poolService = new TxPoolService(pool, _accountContextService, _transactionManager,
                _transactionResultManager);
            poolService.Start();

            var addr11 = Hash.Generate();

            var tx1 = TxPoolTest.BuildTransaction();
            var res = await poolService.AddTxAsync(tx1);
            Assert.Equal(TxValidation.TxInsertionAndBroadcastingError.Success, res);

            Assert.Equal(0, (int) await poolService.GetWaitingSizeAsync());
            Assert.Equal(1, (int) await poolService.GetExecutableSizeAsync());
            Assert.Equal(1, (int) pool.Size);
        }

        [Fact]
        public async Task WaitingTest()
        {
            var pool = GetPool();

            var poolService = new TxPoolService(pool, _accountContextService, _transactionManager,
                _transactionResultManager);
            poolService.Start();
            var tx1 = TxPoolTest.BuildTransaction();
            var res = await poolService.AddTxAsync(tx1);
            
            var tx2 = TxPoolTest.BuildTransaction(nonce:2);
            res = await poolService.AddTxAsync(tx2);
            
            Assert.Equal(TxValidation.TxInsertionAndBroadcastingError.Success, res);

            Assert.Equal(1, (int) await poolService.GetWaitingSizeAsync());
            Assert.Equal(1, (int) await poolService.GetExecutableSizeAsync());
            Assert.Equal(2, (int)pool.Size);

        }


        [Fact]
        public async Task ReadyTxs()
        {
            var pool = GetPool();


            var poolService = new TxPoolService(pool, _accountContextService, _transactionManager,
                _transactionResultManager);
            poolService.Start();

            var keyPair = new KeyPairGenerator().Generate();
            var tx1 = TxPoolTest.BuildTransaction(keyPair: keyPair);
            var tx2 = TxPoolTest.BuildTransaction(nonce: 1, keyPair:keyPair);
            await poolService.AddTxAsync(tx1);
            await poolService.AddTxAsync(tx2);
            var txs1 = await poolService.GetReadyTxsAsync(10);
            Assert.Equal(2, txs1.Count);

            var txResults1 = txs1.Select(t => new TransactionResult
            {
                TransactionId = t.GetHash()
            }).ToList();

            await poolService.ResetAndUpdate(txResults1);

            var addr1 = keyPair.GetAddress();
            var context1 = await _accountContextService.GetAccountDataContext(addr1, pool.ChainId);
            Assert.Equal(2, (int)context1.IncrementId);

            
            var tx3 = TxPoolTest.BuildTransaction(nonce:2, keyPair:keyPair);
            var tx4 = TxPoolTest.BuildTransaction(nonce: 3, keyPair:keyPair);
            
            await poolService.AddTxAsync(tx3);
            await poolService.AddTxAsync(tx4);
            
            var txs2 = await poolService.GetReadyTxsAsync(10);
            Assert.Equal(2, txs2.Count);

            var txResults2 = txs2.Select(t => new TransactionResult
            {
                TransactionId = t.GetHash()
            }).ToList();

            await poolService.ResetAndUpdate(txResults2);
            var context2 = await _accountContextService.GetAccountDataContext(addr1, pool.ChainId);
            Assert.Equal(4, (int)context2.IncrementId);

        }

        [Fact]
        public async Task StopTest()
        {
            var pool = GetPool();

            var poolService = new TxPoolService(pool, _accountContextService, _transactionManager,
                _transactionResultManager);
            poolService.Start();
            
            await poolService.Stop();

            var tx = TxPoolTest.BuildTransaction();
            var res = await poolService.AddTxAsync(tx);
            Assert.Equal(TxValidation.TxInsertionAndBroadcastingError.PoolClosed, res);
        }
    }
}