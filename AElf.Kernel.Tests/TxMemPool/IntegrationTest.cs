using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using AElf.Cryptography.ECDSA;
using AElf.ChainController;
using AElf.ChainController.EventMessages;
using AElf.SmartContract;
using AElf.Kernel.Managers;
using AElf.Kernel.TxMemPool;
using AsyncEventAggregator;
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
        private IStateDictator _stateDictator;

        public IntegrationTest(ILogger logger, IStateDictator stateDictator)
        {
            _logger = logger;
            _stateDictator = stateDictator;
            _stateDictator.BlockProducerAccountAddress = Hash.Generate();
            _accountContextService = new AccountContextService(stateDictator);
            this.Subscribe<TransactionAddedToPool>(async (t) => { await Task.CompletedTask; });
        }
        
        private ContractTxPool GetContractTxPool(ITxPoolConfig config)
        {
            _stateDictator.ChainId = config.ChainId;
            return new ContractTxPool(config, _logger);
        }
        
        private DPoSTxPool GetDPoSTxPool(ITxPoolConfig config)
        {
            _stateDictator.ChainId = config.ChainId;
            return new DPoSTxPool(config, _logger);
        }

        public static Transaction BuildTransaction(Hash adrTo = null, ulong nonce = 0, ECKeyPair keyPair = null, TransactionType type = TransactionType.ContractTransaction)
        {
            
            keyPair = keyPair ?? new KeyPairGenerator().Generate();

            var tx = new Transaction();
            tx.From = keyPair.GetAddress();
            tx.To = adrTo == null ? Hash.Generate().ToAccount() : adrTo;
            tx.IncrementId = nonce;
            tx.P = ByteString.CopyFrom(keyPair.PublicKey.Q.GetEncoded());
            tx.Fee = TxPoolConfig.Default.FeeThreshold + 1;
            tx.MethodName = "hello world";
            tx.Type = type;
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
            var config = TxPoolConfig.Default;
            var pool = GetContractTxPool(config);
            var dPoSPool = GetDPoSTxPool(config);
            var poolService = new TxPoolService(pool, _accountContextService, _logger, dPoSPool);
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
            
            var txList = new List<Transaction>();

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
                    var txs = await poolService.GetReadyTxsAsync();

                    var resLists =new List<TransactionResult>();
                    foreach (var t in txs)
                    {
                        resLists.Add(new TransactionResult
                        {
                            TransactionId = t.GetHash()
                        });
                    }
                    await poolService.UpdateAccountContext(new HashSet<Hash>(addrList.Select(kp=> new Hash(kp.GetAddress()))));
                }
                
                j1++;
            }
        }
        
        //[Fact(Skip = "todo")]
        [Fact]
        public async Task StartMultiThread()
        {
            //var chainId = Hash.Generate();
            //var chain = CreateChain(chainId);
            var config = TxPoolConfig.Default;
            var contractTxPool = GetContractTxPool(config);
            var dPoSPool = GetDPoSTxPool(config);

            _stateDictator.ChainId = TxPoolConfig.Default.ChainId;
            _accountContextService = new AccountContextService(_stateDictator);

            var poolService = new TxPoolService(contractTxPool, _accountContextService, _logger, dPoSPool);

            poolService.Start();
            
            var results = new List<TransactionResult>();

            var idDict = new Dictionary<Hash, ulong>();
            int k = 0;
            
            // address number
            var Num = 100;
            
            var r = 5;
            
            // tx number
            var txNum = Num *r;

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
            
            var txList = new List<Transaction>();
            
            while (count++ < txNum)
            {
                var index = count % Num;
                var id =  new Random().Next(r);
                sortedSet[addrList[index].GetAddress()].Add(id);
                var tx = BuildTransaction(keyPair: addrList[index], nonce: (ulong) id,
                    type: index == 0
                        ? TransactionType.DposTransaction
                        : TransactionType.ContractTransaction);
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
                            var txs = await poolService.GetReadyTxsAsync();

                            var resLists =new List<TransactionResult>();
                            foreach (var t in txs)
                            {
                                resLists.Add(new TransactionResult
                                {
                                    TransactionId = t.GetHash()
                                });
                            }
                            await poolService.UpdateAccountContext(new HashSet<Hash>(addrList.Select(kp=> new Hash(kp.GetAddress()))));
                        }
                    });
                    tasks.Add(task);
                }
                Task.WaitAll(tasks.ToArray());
                
                rr++;
            }
            
            var sortedCount = sortedSet.Values.Aggregate(0, (current, p) => current + p.Count);
            Assert.True(sortedCount >= (int)contractTxPool.Size);

            var list = await poolService.GetReadyTxsAsync();

            var txReuslts = list.Select(t => new TransactionResult
            {
                TransactionId = t.GetHash()
            }).ToList();
            
            await poolService.UpdateAccountContext(new HashSet<Hash>(addrList.Select(kp=> new Hash(kp.GetAddress()))));

            int cc = 0;
            foreach (var address in addrList)
            {
                IPool pool;
                if (cc++ % Num == 0)
                    pool = dPoSPool;
                else
                {
                    pool = contractTxPool;
                }
                
                // pool state
                Assert.Equal(idDict[address.GetAddress()], pool.GetNonce(address.GetAddress()));
                
                // account state
                Assert.Equal(idDict[address.GetAddress()],
                    (await _accountContextService.GetAccountDataContext(address.GetAddress(), config.ChainId)).IncrementId);
            }
        }
        
        
        
    }
}