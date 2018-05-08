using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AElf.Kernel.KernelAccount;
using AElf.Kernel.TxMemPool;
using Xunit;
using Xunit.Frameworks.Autofac;

namespace AElf.Kernel.Tests.TxPool
{
    [UseAutofacTestFramework]
    public class TxPoolServiceTest
    {
        private readonly IAccountContextService _accountContextService;
        private readonly ISmartContractZero _smartContractZero;
        
        public TxPoolServiceTest(IAccountContextService accountContextService, ISmartContractZero smartContractZero)
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
        public async Task AddTxTest()
        {
            var pool = GetPool();
            
            var poolService = new TxPoolService(pool);
            poolService.Start();
           
            var addr11 = Hash.Generate();
            var addr12 = Hash.Generate();
            var addr21 = Hash.Generate();
             var addr22 = Hash.Generate();
            
            var tx1 = new Transaction
            {
                From = addr11,
                To = addr12,
                IncrementId = 0
            };
            var res = await poolService.AddTxAsync(tx1);
            Assert.True(res);
            
            Assert.Equal(1, (int)poolService.GetTmpSizeAsync().Result);
            Assert.Equal(0, (int)poolService.GetWaitingSizeAsync().Result);
            Assert.Equal(0, (int)poolService.GetExecutableSizeAsync().Result);
            Assert.Equal(1, (int)pool.Size);
            
            var tx2 = new Transaction
            {
                From = addr21,
                To = addr22,
                IncrementId = 0
            };
            res = await poolService.AddTxAsync(tx2);
            
            Assert.True(res);
            Assert.Equal(2, (int)poolService.GetTmpSizeAsync().Result);
            Assert.Equal(0, (int)poolService.GetWaitingSizeAsync().Result);
            Assert.Equal(0, (int)poolService.GetExecutableSizeAsync().Result);
            Assert.Equal(2, (int)pool.Size);
            
            
            var tx3 = new Transaction
            {
                From = addr11,
                To = Hash.Generate(),
                IncrementId = 0
            };
            res = await poolService.AddTxAsync(tx3);
            
            Assert.True(res);
            Assert.Equal(3, (int)poolService.GetTmpSizeAsync().Result);
            Assert.Equal(0, (int)poolService.GetWaitingSizeAsync().Result);
            Assert.Equal(0, (int)poolService.GetExecutableSizeAsync().Result);
            Assert.Equal(3, (int)pool.Size);
            
            
            var tx4 = new Transaction
            {
                From = addr11,
                To = Hash.Generate(),
                IncrementId = 1
            };
            res = await poolService.AddTxAsync(tx4);
            Assert.True(res);
            Assert.Equal(4, (int)poolService.GetTmpSizeAsync().Result);
            Assert.Equal(0, (int)poolService.GetWaitingSizeAsync().Result);
            Assert.Equal(0, (int)poolService.GetExecutableSizeAsync().Result);
            Assert.Equal(4, (int)pool.Size);
            
            var tx5 = new Transaction
            {
                From = addr11,
                To = Hash.Generate(),
                IncrementId = 2
            };
            res = await poolService.AddTxAsync(tx5);
            Assert.True(res);
            Thread.Sleep(1000);
            Assert.Equal(0, (int)poolService.GetTmpSizeAsync().Result);
            Assert.Equal(5, (int) poolService.GetPoolSize().Result);
            Assert.Equal(4, (int)poolService.GetWaitingSizeAsync().Result);
            Assert.Equal(0, (int)poolService.GetExecutableSizeAsync().Result);
            
            var tx6 = new Transaction
            {
                From = addr11,
                To = Hash.Generate(),
                IncrementId = 2
            };
            res = await poolService.AddTxAsync(tx6);
            Assert.True(res);
            Assert.Equal(1, (int)poolService.GetTmpSizeAsync().Result);
            Assert.Equal(4, (int)poolService.GetWaitingSizeAsync().Result);
            Assert.Equal(0, (int)poolService.GetExecutableSizeAsync().Result);
            
            await poolService.Stop();
            
            var tx7 = new Transaction
            {
                From = addr11,
                To = Hash.Generate(),
                IncrementId = 3
            };
            res = await poolService.AddTxAsync(tx7);
            Assert.False(res);
            
        }

        
        
        [Fact]
        public async Task IntergrationTest()
        {
            var pool = new TxMemPool.TxPool(new ChainContext(_smartContractZero, Hash.Generate()), TxPoolConfig.Default,
                _accountContextService);
            
            var poolService = new TxPoolService(pool);
            poolService.Start();
            ulong queued = 0;
            ulong exec = 0;
            var tasks = new List<Task>();
            int k = 0;
            var threadNum = 10;
            for (var j = 0; j < 10; j++)
            {
                var task = Task.Run(async () =>
                {
                    var sortedSet = new SortedSet<ulong>();
                    var addr = Hash.Generate();
                    var i = 0;
                    while (i++ < 500)
                    {
                        var id = (ulong) new Random().Next(100);
                        sortedSet.Add(id);
                        var tx = new Transaction
                        {
                            From = addr,
                            To = Hash.Generate(),
                            IncrementId = id
                        };

                        await poolService.AddTxAsync(tx);
                    }

                    ulong c = 0;
                    foreach (var t in sortedSet)
                    {
                        if (t != c)
                            break;
                        c++;
                    }
                    
                    lock (this)
                    {
                        queued += (ulong) sortedSet.Count;
                        exec += c;
                        k++;
                    }
                });
                tasks.Add(task);
            }

            Task.WaitAll(tasks.ToArray());
            pool.QueueTxs();
            await poolService.PromoteAsync();
            Assert.Equal(k, threadNum);
            Assert.Equal(exec, poolService.GetExecutableSizeAsync().Result);
            Assert.Equal(queued - exec, poolService.GetWaitingSizeAsync().Result);
            
        }
        
    }
}