using AElf.Kernel.KernelAccount;
using AElf.Kernel.TxMemPool;
using Xunit;
using Xunit.Frameworks.Autofac;

namespace AElf.Kernel.Tests.TxPool
{
    [UseAutofacTestFramework]
    public class TxPoolTest
    {
        private readonly IAccountContextService _accountContextService;
        private readonly ISmartContractZero _smartContractZero;
        
        public TxPoolTest(IAccountContextService accountContextService, ISmartContractZero smartContractZero)
        {
            _accountContextService = accountContextService;
            _smartContractZero = smartContractZero;
        }

        private TxMemPool.TxPool GetPool()
        {
            return new TxMemPool.TxPool(new ChainContext(_smartContractZero, Hash.Generate()), TxPoolConfig.Default,
                _accountContextService);
        }

        [Fact]
        public void AddTxTest()
        {
            var pool = GetPool();
            var tx = new Transaction();
            var addr11 = Hash.Generate();
            var addr12 = Hash.Generate();
            tx.From = addr11;
            tx.To = addr12;
            tx.IncrementId = 0;
            var res = pool.AddTx(tx);
            Assert.True(res);
            
            res = pool.AddTx(tx);
            Assert.False(res);
            Assert.Equal(1, (int)pool.Size);
            res = pool.Contains(tx.GetHash());
            Assert.True(res);
            
            pool.GetPoolStates(out var executable, out var waiting, out var tmp);
            Assert.Equal(1, (int)tmp);
            Assert.Equal(0, (int)waiting);
            Assert.Equal(0, (int)executable);
        }


        [Fact]
        public void QueueTxTest()
        {
            var pool = GetPool();
            var tx = new Transaction();
            var addr11 = Hash.Generate();
            var addr12 = Hash.Generate();
            tx.From = addr11;
            tx.To = addr12;
            tx.IncrementId = 0;
            var res = pool.AddTx(tx);
            Assert.True(res);
            
            pool.QueueTxs();
            pool.GetPoolStates(out var executable, out var waiting, out var tmp);
            Assert.Equal(0, (int)tmp);
            Assert.Equal(1, (int)waiting);
            Assert.Equal(0, (int)executable);
        }


        [Fact]
        public void PromoteTest()
        {
            var pool = GetPool();
            var tx = new Transaction();
            var addr11 = Hash.Generate();
            var addr12 = Hash.Generate();
            tx.From = addr11;
            tx.To = addr12;
            tx.IncrementId = 0;
            pool.AddTx(tx);
            pool.QueueTxs();
            pool.Promote();
            pool.GetPoolStates(out var executable, out var waiting, out var tmp);
            Assert.Equal(0, (int)tmp);
            Assert.Equal(0, (int)waiting);
            Assert.Equal(1, (int)executable);
        }

        [Fact]
        public void ReadyTxsTest()
        {
            var pool = GetPool();
            var tx = new Transaction();
            var addr11 = Hash.Generate();
            var addr12 = Hash.Generate();
            tx.From = addr11;
            tx.To = addr12;
            tx.IncrementId = 0;
            pool.AddTx(tx);
            pool.QueueTxs();
            pool.Promote();
            var ready = pool.ReadyTxs();
            Assert.Equal(1, ready.Count);
            Assert.True(ready.Contains(tx));
        }


        [Fact]
        public void GetTxTest()
        {
            var pool = GetPool();
            var tx = new Transaction();
            var addr11 = Hash.Generate();
            var addr12 = Hash.Generate();
            tx.From = addr11;
            tx.To = addr12;
            tx.IncrementId = 0;
            pool.AddTx(tx);
            var t = pool.GetTx(tx.GetHash());
            Assert.Equal(tx, t);
        }
        
    }
}