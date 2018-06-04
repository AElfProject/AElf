using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AElf.Kernel.Crypto.ECDSA;
using AElf.Kernel.KernelAccount;
using AElf.Kernel.Managers;
using AElf.Kernel.Services;
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
        public TxPoolServiceTest(IAccountContextService accountContextService, ILogger logger,
            ITransactionManager transactionManager, ITransactionResultManager transactionResultManager)
        {
            _accountContextService = accountContextService;
            _logger = logger;
            _transactionManager = transactionManager;
            _transactionResultManager = transactionResultManager;
        }

        private TxPool GetPool()
        {
            return new TxPool(TxPoolConfig.Default, _logger);
        }

        [Fact]
        public async Task Serialize()
        {
            var tx = Transaction.Parser.ParseFrom(ByteString.FromBase64(
                @"CiIKIKkqNVMSxCWn/TizqYJl0ymJrnrRqZN+W3incFJX3MRIEiIKIIFxBhlGhI1auR05KafXd/lFGU+apqX96q1YK6aiZLMhIgh0cmFuc2ZlcioJCgcSBWhlbGxvOiEAxfMt77nwSKl/WUg1TmJHfxYVQsygPj0wpZ/Pbv+ZK4pCICzGxsZBCBlASmlDdn0YIv6vRUodJl/9jWd8Q1z2ofFwSkEE+PDQtkHQxvw0txt8bmixMA8lL0VM5ScOYiEI82LX1A6oWUNiLIjwAI0Qh5fgO5g5PerkNebXLPDE2dTzVVyYYw=="));
            var pool = GetPool();
            var keypair = new KeyPairGenerator().Generate();
            var poolService = new TxPoolService(pool, _accountContextService, _transactionManager,
                _transactionResultManager);
            poolService.Start();
            await poolService.AddTxAsync(tx);
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
            Assert.True(res);

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
            
            Assert.True(res);
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

            var addr1 = Hash.Generate();
            var tx1 = TxPoolTest.BuildTransaction(adrFrom:addr1);
            var tx2 = TxPoolTest.BuildTransaction(adrFrom: addr1, nonce: 1);
            await poolService.AddTxAsync(tx1);
            await poolService.AddTxAsync(tx2);
            var txs1 = await poolService.GetReadyTxsAsync(10);
            Assert.Equal(2, txs1.Count);

            var txResults1 = txs1.Select(t => new TransactionResult
            {
                TransactionId = t.GetHash()
            }).ToList();

            await poolService.ResetAndUpdate(txResults1);
            
            var context1 = await _accountContextService.GetAccountDataContext(addr1, pool.ChainId);
            Assert.Equal(2, (int)context1.IncrementId);

            
            var tx3 = TxPoolTest.BuildTransaction(adrFrom:addr1, nonce:2);
            var tx4 = TxPoolTest.BuildTransaction(adrFrom: addr1, nonce: 3);
            
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
            Assert.False(res);
        }
    }
}