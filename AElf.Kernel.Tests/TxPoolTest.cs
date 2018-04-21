using System;
using System.Runtime.InteropServices.ComTypes;
using System.Threading;
using System.Threading.Tasks;
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
            var addr11 = Hash.Generate();
            var addr12 = Hash.Generate();
            tx.From = addr11;
            tx.To = addr12;
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

            var t = pool.GetTransaction(tx.GetHash());
            Assert.Equal(tx, t);

            res = pool.AddTx(t);
            Assert.False(res);

            var addr21 = Hash.Generate();
            var addr22 = Hash.Generate();
            
            var tx2 = new Transaction
            {
                From = addr21,
                To = addr22,
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
            
            var tx3 = new Transaction
            {
                From = addr11,
                To = Hash.Generate(),
                IncrementId = 0
            };
            res = pool.AddTx(tx3);
            Assert.True(res);
            pool.GetPoolStates(out executable, out waiting);
            Assert.Equal(1, (int)waiting);
            Assert.Equal(2, (int)executable);
            
            
            var tx4 = new Transaction
            {
                From = addr11,
                To = Hash.Generate(),
                IncrementId = 0
            };
            res = pool.AddTx(tx4);
            Assert.True(res);
            pool.GetPoolStates(out executable, out waiting);
            Assert.Equal(1, (int)waiting);
            Assert.Equal(2, (int)executable);
            Assert.Equal(4, (int)pool.Size);
            
            var tx5 = new Transaction
            {
                From = addr11,
                To = Hash.Generate(),
                IncrementId = 1
            };
            res = pool.AddTx(tx5);
            Assert.True(res);
            pool.GetPoolStates(out executable, out waiting);
            Assert.Equal(2, (int)waiting);
            Assert.Equal(2, (int)executable);
            Assert.Equal(5, (int)pool.Size);
            
            pool.Promote();
            pool.GetPoolStates(out executable, out waiting);
            Assert.Equal(0, (int)waiting);
            Assert.Equal(4, (int)executable);
            Assert.Equal(5, (int)pool.Size);
            
            var tx6 = new Transaction
            {
                From = addr11,
                To = Hash.Generate(),
                IncrementId = 2
            };
            res = pool.AddTx(tx6);
            Assert.True(res);
            pool.GetPoolStates(out executable, out waiting);
            Assert.Equal(1, (int)waiting);
            Assert.Equal(4, (int)executable);
            Assert.Equal(6, (int)pool.Size);
            
            
            res = pool.DisgardTx(tx6.GetHash());
            Assert.True(res);
            pool.GetPoolStates(out executable, out waiting);
            Assert.Equal(0, (int)waiting);
            Assert.Equal(4, (int)executable);
            Assert.Equal(5, (int)pool.Size);

            res = pool.DisgardTx(tx.GetHash());
            Assert.True(res);
            pool.GetPoolStates(out executable, out waiting);
            Assert.Equal(2, (int)waiting);
            Assert.Equal(1, (int)executable);
            Assert.Equal(4, (int)pool.Size);
            
            res = pool.DisgardTx(tx.GetHash());
            Assert.False(res);
            
            pool.ClearAll();
            Assert.Equal(0, (int)pool.Size);

            Assert.Equal(0, (int)pool.Size);

        }


        [Fact]
        public async Task TxPoolServiceTest()
        {
            var pool = new TxPool(new ChainContext(_smartContractZero, Hash.Generate()), TxPoolConfig.Default,
                _accountContextService);
            
            var poolService = new TxPoolService(pool, _transactionManager);
            poolService.Start();
           
            var addr11 = Hash.Generate();
            var addr12 = Hash.Generate();
            var tx1 = new Transaction
            {
                From = addr11,
                To = addr12,
                IncrementId = 0
            };
            var res = await poolService.AddTransaction(tx1);
            
            Assert.True(res);
            Assert.Equal(1, poolService.GetTmpPoolSize().Result);
            pool.GetPoolStates(out var executable, out var waiting);
            Assert.Equal(0, (int)waiting);
            Assert.Equal(0, (int)executable);
            Assert.Equal(0, (int)pool.Size);
            
            var addr21 = Hash.Generate();
            var addr22 = Hash.Generate();
            var tx2 = new Transaction
            {
                From = addr21,
                To = addr22,
                IncrementId = 0
            };
            res = await poolService.AddTransaction(tx2);
            
            Assert.True(res);
            Assert.Equal(2, poolService.GetTmpPoolSize().Result);
            pool.GetPoolStates(out executable, out waiting);
            Assert.Equal(0, (int)waiting);
            Assert.Equal(0, (int)executable);
            Assert.Equal(0, (int)pool.Size);
            
            
            var tx3 = new Transaction
            {
                From = addr11,
                To = Hash.Generate(),
                IncrementId = 0
            };
            res = await poolService.AddTransaction(tx3);
            
            Assert.True(res);
            Assert.Equal(3, poolService.GetTmpPoolSize().Result);
            pool.GetPoolStates(out executable, out waiting);
            Assert.Equal(0, (int)waiting);
            Assert.Equal(0, (int)executable);
            Assert.Equal(0, (int)pool.Size);
            
            
            var tx4 = new Transaction
            {
                From = addr11,
                To = Hash.Generate(),
                IncrementId = 1
            };
            res = await poolService.AddTransaction(tx4);
            Assert.True(res);
            Assert.Equal(4, poolService.GetTmpPoolSize().Result);
            pool.GetPoolStates(out executable, out waiting);
            Assert.Equal(0, (int)waiting);
            Assert.Equal(0, (int)executable);
            Assert.Equal(0, (int)pool.Size);
            
            var tx5 = new Transaction
            {
                From = addr11,
                To = Hash.Generate(),
                IncrementId = 2
            };
            res = await poolService.AddTransaction(tx5);
            Assert.True(res);
            Thread.Sleep(3000);
            Assert.Equal(0, poolService.GetTmpPoolSize().Result);
            Assert.Equal(5, (int) poolService.GetPoolSize().Result);
            pool.GetPoolStates(out executable, out waiting);
            Assert.Equal(4, (int)waiting);
            Assert.Equal(0, (int)executable);
            
            var tx6 = new Transaction
            {
                From = addr11,
                To = Hash.Generate(),
                IncrementId = 2
            };
            res = await poolService.AddTransaction(tx6);
            Assert.True(res);
            Thread.Sleep(3000);
            Assert.Equal(1, poolService.GetTmpPoolSize().Result);
            pool.GetPoolStates(out executable, out waiting);
            Assert.Equal(4, (int)waiting);
            Assert.Equal(0, (int)executable);
            
            
        }
        
    }
}