using AElf.Kernel.KernelAccount;
using AElf.Kernel.Services;
using AElf.Kernel.TxMemPool;
using Xunit;
using Xunit.Frameworks.Autofac;

namespace AElf.Kernel.Tests.TxMemPool
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

        private TxPool GetPool()
        {
            return new TxPool(new ChainContext(_smartContractZero, Hash.Generate()), TxPoolConfig.Default,
                _accountContextService);
        }

        private Transaction BuildTransaction(Hash adrFrom = null, Hash adrTo = null, ulong nonce = 0)
        {
            
            var tx = new Transaction();
            tx.From = adrFrom == null ? Hash.Generate() : adrFrom;
            tx.To = adrTo == null ? Hash.Generate() : adrTo;
            tx.IncrementId = nonce;

            return tx;
        }
        
        [Fact]
        public void EntryThreshold_Test()
        {
            // setup config
            TxPoolConfig conf = TxPoolConfig.Default;
            conf.EntryThreshold = 1;

            var pool = new TxPool(new ChainContext(_smartContractZero, Hash.Generate()), conf, _accountContextService);
            
            // Add a valid transaction
            var tx = BuildTransaction();
            pool.AddTx(tx);
            
            pool.GetPoolStates(out var executable, out var waiting, out var tmp);
            
            Assert.Equal(1, (int)tmp);
            Assert.Equal(0, (int)waiting);
            Assert.Equal(0, (int)executable);
        }

        // Adding a valide transaction to the pool => AddTx returns true
        [Fact]
        public void AddTx_ValidTransaction_ReturnsTrue()
        {
            TxPool pool = GetPool();
            var tx = BuildTransaction();
            
            bool addResult = pool.AddTx(tx);
            
            Assert.True(addResult);
        }

        [Fact]
        public void ContainsTx_ReturnsTrue_AfterAdd()
        {
            TxPool pool = GetPool();
            var tx = BuildTransaction();
            
            pool.AddTx(tx);

            bool res = pool.Contains(tx.GetHash());
            
            Assert.True(res);
        }

        [Fact]
        public void QueueTxTest()
        {
            TxPool pool = GetPool();
            var tx = BuildTransaction();
            
            pool.AddTx(tx);
            pool.QueueTxs();
            
            pool.GetPoolStates(out var executable, out var waiting, out var tmp);
            
            Assert.Equal(0, (int)tmp);
            Assert.Equal(1, (int)waiting);
            Assert.Equal(0, (int)executable);
        }

        [Fact]
        public void PromoteTest()
        {
            TxPool pool = GetPool();
            var tx = BuildTransaction();
            
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
            TxPool pool = GetPool();
            var tx = BuildTransaction();
            
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
            TxPool pool = GetPool();
            var tx = BuildTransaction();
            
            pool.AddTx(tx);
            
            var t = pool.GetTx(tx.GetHash());
            
            Assert.Equal(tx, t);
        }
    }
}