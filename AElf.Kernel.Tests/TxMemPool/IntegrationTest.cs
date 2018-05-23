using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Kernel.KernelAccount;
using AElf.Kernel.Services;
using AElf.Kernel.TxMemPool;
using Xunit;
using Xunit.Frameworks.Autofac;

namespace AElf.Kernel.Tests.TxMemPool
{
    [UseAutofacTestFramework]
    public class IntegrationTest
    {
        private readonly IAccountContextService _accountContextService;

        public IntegrationTest(IAccountContextService accountContextService)
        {
            _accountContextService = accountContextService;
        }
        
        private TxPool GetPool()
        {
            return new TxPool(TxPoolConfig.Default);
        }

        [Fact]
        public async Task Start()
        {
            var pool = GetPool();
            
            var poolService = new TxPoolService(pool, _accountContextService);
            poolService.Start();
            ulong queued = 0;
            ulong exec = 0;
            var tasks = new List<Task>();
            int k = 0;
            var threadNum = 10;
            for (var j = 0; j < threadNum; j++)
            {
                var task = Task.Run(async () =>
                {
                    // sorted set for tx id
                    var sortedSet = new SortedSet<ulong>();
                    var addr = Hash.Generate();
                    var i = 0;
                    while (i++ < 100)
                    {
                        var id = (ulong) new Random().Next(10);
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
            await poolService.PromoteAsync();
            Assert.Equal(k, threadNum);
            Assert.Equal(exec, await poolService.GetExecutableSizeAsync());
            Assert.Equal(queued - exec, await poolService.GetWaitingSizeAsync());
            
        }
    }
}