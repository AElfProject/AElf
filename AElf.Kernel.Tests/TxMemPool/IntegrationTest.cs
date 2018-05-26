using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using AElf.Kernel.Crypto.ECDSA;
using AElf.Kernel.KernelAccount;
using AElf.Kernel.Services;
using AElf.Kernel.TxMemPool;
using Akka.Actor;
using Akka.Util;
using Google.Protobuf;
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

        public static Transaction BuildTransaction(Hash adrFrom = null, Hash adrTo = null, ulong nonce = 0)
        {
            
            ECKeyPair keyPair = new KeyPairGenerator().Generate();

            var tx = new Transaction();
            tx.From = adrFrom == null ? Hash.Generate() : adrFrom;
            tx.To = adrTo == null ? Hash.Generate() : adrTo;
            tx.IncrementId = nonce;
            tx.P = ByteString.CopyFrom(keyPair.PublicKey.Q.GetEncoded());
            tx.Fee = TxPoolConfig.Default.FeeThreshold + 1;
            tx.MethodName = "hello world";
            tx.Params = ByteString.CopyFrom(new Parameters
            {
                Params = { new Param
                {
                    IntVal = 1
                }}
            }.ToByteArray());

            // Serialize and hash the transaction
            Hash hash = tx.GetHash();
            
            // Sign the hash
            ECSigner signer = new ECSigner();
            ECSignature signature = signer.Sign(keyPair, hash.GetHashBytes());
            
            // Update the signature
            tx.R = ByteString.CopyFrom(signature.R);
            tx.S = ByteString.CopyFrom(signature.S);
            
            return tx;
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
            
            var results = new List<TransactionResult>();

            var IdDict = new Dictionary<Hash, ulong>();
            int k = 0;
            var Num = 50;
            var threadNum = 50;

            int count = 0;
            
            var addrList = new List<Hash>();

            var sortedSet = new Dictionary<Hash, SortedSet<int>>();

            int i = 0;
            while (i < Num )
            {
                var addr = Hash.Generate();
                addrList.Add(addr);
                sortedSet[addr] = new SortedSet<int>();
                i++;
            }
            
            var txList = new List<ITransaction>();
            
            while (count++ < threadNum)
            {
                var index = count % Num;
                var id =  new Random().Next(50);
                sortedSet[addrList[index]].Add(id);
                var tx = BuildTransaction(addrList[index], nonce: (ulong)id);
                txList.Add(tx);
            }

            foreach (var addr in addrList)
            {
                ulong c = 0;
                foreach (var t in sortedSet[addr])
                {
                    if (t != (int)c)
                        break;
                    c++;
                }
                IdDict[addr] = c;
            }
            
            for (var j = 0; j < threadNum; j++)
            {
                var j1 = j;
                var task = Task.Run(async () =>
                {
                    if (j1 % Num == 0)
                    {
                        await poolService.PromoteAsync();
                    }
                    
                    // sorted set for tx id
                    var stopwatch = new Stopwatch();
                    stopwatch.Start();
                    
                    var res = await poolService.AddTxAsync(txList[j1]);
                    results.Add(new TransactionResult
                    {
                        TransactionId = txList[j1].GetHash()
                    });
                    stopwatch.Stop();
                    Debug.WriteLine(stopwatch.ElapsedMilliseconds);
                    
                });
                tasks.Add(task);
            }

            Task.WaitAll(tasks.ToArray());
            
            Assert.Equal(sortedSet.Values.Aggregate(0, (current, p) =>  current + p.Count), (int)pool.Size);

            await poolService.PromoteAsync();

            // executable list size 
            /*Assert.Equal(exec, await poolService.GetExecutableSizeAsync());
            Assert.Equal(queued - exec, await poolService.GetWaitingSizeAsync());*/

            var list = await poolService.GetReadyTxsAsync(2000);

            await poolService.ResetAndUpdate(results);
            
            foreach (var address in addrList)
            {
                // pool state
                Assert.Equal(IdDict[address], pool.Nonces[address]);
                
                // account state
                Assert.Equal(IdDict[address],
                    (await _accountContextService.GetAccountDataContext(address, pool.ChainId)).IncrementId);
            }
        }
        
        
        public void StartNoLock()
        {
            /*var pool = GetPool();
            
            var poolService = new TxPoolNoLockService(pool, _accountContextService);
            poolService.Start();
            ulong queued = 0;
            ulong exec = 0;
            var tasks = new List<Task>();
            int k = 0;
            var threadNum = 10;
            for (var j = 0; j < threadNum; j++)
            {
                var task = Task.Run( () =>
                {
                    // sorted set for tx id
                    var sortedSet = new SortedSet<ulong>();
                    var addr = Hash.Generate();
                    var i = 0;
                    while (i++ < 100)
                    {
                        var id = (ulong) new Random().Next(50);
                        sortedSet.Add(id);
                        var tx = new Transaction
                        {
                            From = addr,
                            To = Hash.Generate(),
                            IncrementId = id
                        };

                        poolService.AddTx(tx);
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
            //poolService.Promote();
            Assert.Equal(k, threadNum);
            Assert.Equal(exec, poolService.GetExecutableSize());
            Assert.Equal(queued - exec, poolService.GetWaitingSize());*/
        }
    }
}