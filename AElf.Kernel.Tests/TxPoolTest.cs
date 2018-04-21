using AElf.Kernel.KernelAccount;
using AElf.Kernel.TxMemPool;
using Xunit;
using Xunit.Frameworks.Autofac;

namespace AElf.Kernel.Tests
{
    [UseAutofacTestFramework]
    public class TxPoolTest
    {
        private readonly IChainContextService _chainContextService;
        private readonly ITransactionManager _transactionManager;
        private readonly IAccountContextService _accountContextService;
        private readonly ISmartContractZero _smartContractZero;
        
        public TxPoolTest(IChainContextService chainContextService, ITransactionManager transactionManager, 
            IAccountContextService accountContextService, ISmartContractZero smartContractZero)
        {
            _chainContextService = chainContextService;
            _transactionManager = transactionManager;
            _accountContextService = accountContextService;
            _smartContractZero = smartContractZero;
        }

        [Fact]
        public void PoolTest()
        {
            var tx = new Transaction();
            var addr1 = Hash.Generate();
            var addr2 = Hash.Generate();
            tx.From = addr1;
            tx.To = addr2;
            tx.IncrementId = 0;
            var pool = new TxPool(new ChainContext(_smartContractZero, Hash.Generate()), TxPoolConfig.Default,
                _accountContextService);
            var res = pool.AddTx(tx);
            Assert.True(res);

            res = pool.AddTx(tx);
            Assert.False(res);
            Assert.Equal(1, (int)pool.Size);
            
            res = pool.Contains(tx.GetHash());
            Assert.True(res);
            
            pool.GetPoolStates(out var executable, out var waiting);
            Assert.Equal(1, (int)waiting);
            Assert.Equal(0, (int)executable);
            
            pool.Promote();
            pool.GetPoolStates(out executable, out waiting);
            Assert.Equal(0, (int)waiting);
            Assert.Equal(1, (int)executable);

            var ready = pool.Ready;
            Assert.Equal(1, ready.Count);
            Assert.True(ready.Contains(tx));

            var tx2 = pool.GetTransaction(tx.GetHash());
            Assert.Equal(tx, tx2);

            res = pool.AddTx(tx2);
            Assert.False(res);

            tx2 = new Transaction
            {
                From = Hash.Generate(),
                To = Hash.Generate(),
                IncrementId = 0
            };
            res = pool.AddTx(tx2);
            Assert.True(res);
            
            pool.GetPoolStates(out executable, out waiting);
            Assert.Equal(1, (int)waiting);
            Assert.Equal(1, (int)executable);
            
            pool.Promote();
            pool.GetPoolStates(out executable, out waiting);
            Assert.Equal(0, (int)waiting);
            Assert.Equal(2, (int)executable);
            
            tx2 = new Transaction
            {
                From = addr1,
                To = Hash.Generate(),
                IncrementId = 0
            };
            res = pool.AddTx(tx2);
            Assert.True(res);
            
            pool.ClearAll();
            Assert.Equal(0, (int)pool.Size);

            res = pool.DisgardTx(tx.GetHash());
            Assert.False(res);
            Assert.Equal(0, (int)pool.Size);

        }
        
    }
}