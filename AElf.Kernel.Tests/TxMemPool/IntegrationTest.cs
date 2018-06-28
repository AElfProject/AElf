using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.ComTypes;
using System.Threading.Tasks;
using AElf.Cryptography.ECDSA;
using AElf.Kernel.KernelAccount;
using AElf.Kernel.Managers;
using AElf.Kernel.Services;
using AElf.Kernel.TxMemPool;
using Akka.Actor;
using Akka.Util;
using Google.Protobuf;
using NLog;
using ServiceStack;
using Xunit;
using Xunit.Frameworks.Autofac;

namespace AElf.Kernel.Tests.TxMemPool
{
    [UseAutofacTestFramework]
    public class IntegrationTest
    {
        private IAccountContextService _accountContextService;
        private readonly ILogger _logger;
        private readonly ITransactionManager _transactionManager;
        private readonly ITransactionResultManager _transactionResultManager;
        private readonly IChainCreationService _chainCreationService;
        private IBlockManager _blockManager;
        private IWorldStateDictator _worldStateDictator;

        public IntegrationTest(ILogger logger,
            ITransactionManager transactionManager, ITransactionResultManager transactionResultManager, 
            IChainCreationService chainCreationService, IBlockManager blockManager, 
            IWorldStateDictator worldStateDictator)
        {
            _logger = logger;
            _transactionManager = transactionManager;
            _transactionResultManager = transactionResultManager;
            _chainCreationService = chainCreationService;
            _blockManager = blockManager;
            _worldStateDictator = worldStateDictator;
            
            _accountContextService = new AccountContextService(worldStateDictator);

        }
        
        private TxPool GetPool(Hash chainId = null)
        {
            var config = TxPoolConfig.Default;
            _worldStateDictator.SetChainId(config.ChainId);
            return new TxPool(config, _logger);
        }

        public static Transaction BuildTransaction(Hash adrTo = null, ulong nonce = 0, ECKeyPair keyPair = null)
        {
            
            keyPair = keyPair ?? new KeyPairGenerator().Generate();

            var tx = new Transaction();
            tx.From = keyPair.GetAddress();
            tx.To = adrTo == null ? Hash.Generate().ToAccount() : adrTo;
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
            /*var chainId = Hash.Generate();
            var chain = CreateChain(chainId);*/
            var pool = GetPool();

            var poolService = new TxPoolService(pool, _accountContextService, _transactionManager,
                _transactionResultManager);
            poolService.Start();
            var Num = 3;
            var threadNum = 5;

            int count = 0;
            
            var addrList = new List<ECKeyPair>();

            var sortedSet = new Dictionary<Hash, SortedSet<int>>();

            int r = 5;

            int i = 0;
            while (i < Num )
            {
                var keypair = new KeyPairGenerator().Generate();
                addrList.Add(keypair);
                sortedSet[keypair.GetAddress()] = new SortedSet<int>();
                i++;
            }
            
            var txList = new List<ITransaction>();

            var txCount = threadNum * r;
            while (count++ < txCount)
            {
                var index = count % Num;
                var id =  new Random().Next(25);
                sortedSet[addrList[index].GetAddress()].Add(id);
                var tx = BuildTransaction(keyPair:addrList[index], nonce: (ulong)id);
                txList.Add(tx);
            }

            

            var j1 = 0;
            while (j1 < txCount)
            {
                
                var tx = txList[j1];
                var res = await poolService.AddTxAsync(tx);

                if (j1 % txCount == 0)
                {
                    var txs = await poolService.GetReadyTxsAsync(2000);

                    var resLists =new List<TransactionResult>();
                    foreach (var t in txs)
                    {
                        resLists.Add(new TransactionResult
                        {
                            TransactionId = t.GetHash()
                        });
                    }
                    await poolService.ResetAndUpdate(resLists);
                }
                
                j1++;
            }
        }

        public byte[] SmartContractZeroCode
        {
            get
            {
                byte[] code = null;
                using (FileStream file = File.OpenRead(System.IO.Path.GetFullPath("../../../../AElf.Contracts.SmartContractZero/bin/Debug/netstandard2.0/AElf.Contracts.SmartContractZero.dll")))
                {
                    code = file.ReadFully();
                }
                return code;
            }
        }
        
        
        public async Task<IChain> CreateChain(Hash chainId = null)
        {
            var reg = new SmartContractRegistration
            {
                Category = 0,
                ContractBytes = ByteString.CopyFrom(SmartContractZeroCode),
                ContractHash = Hash.Zero
            };
            var chain = await _chainCreationService.CreateNewChainAsync(chainId, reg);
            var genesis = await _blockManager.GetBlockAsync(chain.GenesisBlockHash);
            return chain;
        }
        
        [Fact]
        public async Task StartMultiThread()
        {
            _worldStateDictator.SetChainId(Hash.Generate());
            _accountContextService = new AccountContextService(_worldStateDictator);

            //var chainId = Hash.Generate();
            //var chain = CreateChain(chainId);
            var pool = GetPool();

            var poolService = new TxPoolService(pool, _accountContextService, _transactionManager,
                _transactionResultManager);
            poolService.Start();
            
            var results = new List<TransactionResult>();

            var idDict = new Dictionary<Hash, ulong>();
            int k = 0;
            var Num = 100;
            var r = 5;
            var threadNum = Num *r;

            int count = 0;
            
            var addrList = new List<ECKeyPair>();

            var sortedSet = new Dictionary<Hash, SortedSet<int>>();

            int i = 0;
            while (i < Num )
            {
                var keypair = new KeyPairGenerator().Generate();
                addrList.Add(keypair);
                sortedSet[keypair.GetAddress()] = new SortedSet<int>();
                i++;
            }
            
            var txList = new List<ITransaction>();
            
            while (count++ < threadNum)
            {
                var index = count % Num;
                var id =  new Random().Next(r);
                sortedSet[addrList[index].GetAddress()].Add(id);
                var tx = BuildTransaction(keyPair:addrList[index], nonce: (ulong)id);
                txList.Add(tx);
            }

            foreach (var addr in addrList)
            {
                ulong c = 0;
                foreach (var t in sortedSet[addr.GetAddress()])
                {
                    if (t != (int)c)
                        break;
                    c++;
                }
                idDict[addr.GetAddress()] = c;
            }

            var rr = 0;
            while (rr< r)
            {
                var tasks = new List<Task>();

                for (var j = 0; j < Num; j++)
                {
                    var j1 = j;
                    var rr1 = rr;
                    var task = Task.Run(async () =>
                    {
                    
                        // sorted set for tx id
                        var stopwatch = new Stopwatch();
                        stopwatch.Start();
                    
                        var res = await poolService.AddTxAsync(txList[j1 + Num * rr1]);
                        results.Add(new TransactionResult
                        {
                            TransactionId = txList[j1].GetHash()
                        });
                        stopwatch.Stop();
                        Debug.WriteLine(stopwatch.ElapsedMilliseconds);
                    
                        if (j1 == Num-1)
                        {
                            var txs = await poolService.GetReadyTxsAsync(2000);

                            var resLists =new List<TransactionResult>();
                            foreach (var t in txs)
                            {
                                resLists.Add(new TransactionResult
                                {
                                    TransactionId = t.GetHash()
                                });
                            }
                            await poolService.ResetAndUpdate(resLists);
                        }
                    });
                    tasks.Add(task);
                }
                Task.WaitAll(tasks.ToArray());
                
                rr++;
            }
            
            
            // var execSize = pool.GetExecutableSize();
            // var waitingSize = pool.GetWaitingSize();
            var sortedCount = sortedSet.Values.Aggregate(0, (current, p) => current + p.Count);
            Assert.True(sortedCount >= (int)pool.Size);

            //await poolService.PromoteAsync();

            // executable list size 
            /*Assert.Equal(exec, await poolService.GetExecutableSizeAsync());
            Assert.Equal(queued - exec, await poolService.GetWaitingSizeAsync());*/

            var list = await poolService.GetReadyTxsAsync(2000);

            var txReuslts = list.Select(t => new TransactionResult
            {
                TransactionId = t.GetHash()
            }).ToList();
            
            await poolService.ResetAndUpdate(txReuslts);
            
            foreach (var address in addrList)
            {
                // pool state
                Assert.Equal(idDict[address.GetAddress()], pool.Nonces[address.GetAddress()]);
                
                // account state
                Assert.Equal(idDict[address.GetAddress()],
                    (await _accountContextService.GetAccountDataContext(address.GetAddress(), pool.ChainId)).IncrementId);
            }
        }
        
        
        
    }
}