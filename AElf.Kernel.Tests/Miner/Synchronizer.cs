using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using AElf.Cryptography.ECDSA;
using AElf.Kernel.Concurrency;
using AElf.Kernel.Concurrency.Execution;
using AElf.Kernel.Concurrency.Execution.Messages;
using AElf.Kernel.Extensions;
using AElf.Kernel.KernelAccount;
using AElf.Kernel.Managers;
using AElf.Kernel.Services;
using AElf.Kernel.Storages;
using AElf.Kernel.Tests.Concurrency.Execution;
using AElf.Kernel.Tests.Concurrency.Scheduling;
using AElf.Kernel.TxMemPool;
using AElf.Runtime.CSharp;
using Akka.Actor;
using Google.Protobuf;
using ServiceStack;
using Xunit;
using Xunit.Frameworks.Autofac;

using Akka.TestKit;
using Akka.TestKit.Xunit;
using Google.Protobuf.WellKnownTypes;
using NLog;
using Org.BouncyCastle.Crypto.Generators;

namespace AElf.Kernel.Tests.Miner
{
    [UseAutofacTestFramework]
    public class Synchronizer : TestKitBase
    {
        private IChainCreationService _chainCreationService;
        private ActorSystem sys = ActorSystem.Create("test");
        private IChainContextService _chainContextService;
        private IWorldStateManager _worldStateManager;
        private ISmartContractManager _smartContractManager;


        public Synchronizer(IWorldStateManager worldStateManager, ISmartContractStore smartContractStore,
            IChainCreationService chainCreationService, IChainContextService chainContextService, IChainManager chainManager, IBlockManager blockManager, ILogger logger, ITransactionResultManager transactionResultManager, ITransactionManager transactionManager, IAccountContextService accountContextService) : base(new XunitAssertions())
        {
            _chainCreationService = chainCreationService;
            _chainContextService = chainContextService;
            _chainManager = chainManager;
            _blockManager = blockManager;
            _logger = logger;
            _transactionResultManager = transactionResultManager;
            _transactionManager = transactionManager;
            _accountContextService = accountContextService;

            _worldStateManager = worldStateManager;
            _smartContractManager = new SmartContractManager(smartContractStore);
        }

        public byte[] SmartContractZeroCode
        {
            get
            {
                return ContractCodes.TestContractZeroCode;
            }
        }
        
        public byte[] ExampleContractCode
        {
            get
            {
                return ContractCodes.TestContractCode;
            }
        }
        

        private static int _incrementId;
        private IChainManager _chainManager;
        private IBlockManager _blockManager;

        
        public ulong NewIncrementId()
        {
            var res = _incrementId;
            var n = Interlocked.Increment(ref _incrementId);
            return (ulong) res;
        }
        
        public Block GenerateBlock(Hash chainId, Hash previousHash, ulong index)
        {
            var block = new Block(previousHash)
            {
                Header = new BlockHeader
                {
                    ChainId = chainId,
                    Index = index,
                    PreviousBlockHash = previousHash,
                    Time = Timestamp.FromDateTime(DateTime.UtcNow)
                }
            };
            
            return block;
        }
        
        public async Task<IChain> CreateChain()
        {
            var chainId = Hash.Generate();
            var reg = new SmartContractRegistration
            {
                Category = 0,
                ContractBytes = ByteString.CopyFrom(SmartContractZeroCode),
                ContractHash = SmartContractZeroCode.CalculateHash()
            };

            var chain = await _chainCreationService.CreateNewChainAsync(chainId, reg);
            return chain;
        }


        public List<ITransaction> CreateTxs(Hash chainId)
        {
            var contractAddressZero = new Hash(chainId.CalculateHashWith("__SmartContractZero__")).ToAccount();

            var code = ExampleContractCode;

            var regExample = new SmartContractRegistration
            {
                Category = 0,
                ContractBytes = ByteString.CopyFrom(code),
                ContractHash = code.CalculateHash()
            };
            
            
            ECKeyPair keyPair = new KeyPairGenerator().Generate();
            ECSigner signer = new ECSigner();
            var txnDep = new Transaction()
            {
                From = keyPair.GetAddress(),
                To = contractAddressZero,
                IncrementId = NewIncrementId(),
                MethodName = "DeploySmartContract",
                Params = ByteString.CopyFrom(new Parameters()
                {
                    Params = {
                        new Param
                        {
                            RegisterVal = regExample
                        }
                    }
                }.ToByteArray()),
                
                Fee = TxPoolConfig.Default.FeeThreshold + 1
            };
            
            Hash hash = txnDep.GetHash();

            ECSignature signature = signer.Sign(keyPair, hash.GetHashBytes());
            txnDep.P = ByteString.CopyFrom(keyPair.PublicKey.Q.GetEncoded());
            txnDep.R = ByteString.CopyFrom(signature.R); 
            txnDep.S = ByteString.CopyFrom(signature.S);
            
            var txs = new List<ITransaction>(){
                txnDep
            };

            return txs;
        }
        
        private readonly IAccountContextService _accountContextService;
        private readonly ILogger _logger;
        private readonly ITransactionManager _transactionManager;
        private readonly ITransactionResultManager _transactionResultManager;
        private ISmartContractRunnerFactory _smartContractRunnerFactory = new SmartContractRunnerFactory();
        private ISmartContractService _smartContractService;
        private IActorRef _generalExecutor;
        private IActorRef _serviceRouter;

        
        [Fact]
        public async Task SyncGenesisBlock()
        {
            var poolconfig = TxPoolConfig.Default;
            var chain = await CreateChain();
            poolconfig.ChainId = chain.Id;
            
            var pool = new TxPool(poolconfig, _logger);
            
            var poolService = new TxPoolService(pool, _accountContextService, _transactionManager,
                _transactionResultManager);
            
            poolService.Start();
            var block = GenerateBlock(chain.Id, chain.GenesisBlockHash, 1);
            
            var txs = CreateTxs(chain.Id);
            foreach (var transaction in txs)
            {
                block.Body.Transactions.Add(transaction.GetHash());
                await poolService.AddTxAsync(transaction);
            }
            
            block.FillTxsMerkleTreeRootInHeader();
            
            var runner = new SmartContractRunner("../../../../AElf.Contracts.Examples/bin/Debug/netstandard2.0/");
            _smartContractRunnerFactory.AddRunner(0, runner);
            _smartContractService = new SmartContractService(_smartContractManager, _smartContractRunnerFactory, await _worldStateManager.OfChain(chain.Id));
            
            _serviceRouter = sys.ActorOf(LocalServicesProvider.Props(new ServicePack
            {
                ChainContextService = _chainContextService,
                SmartContractService = _smartContractService,
                ResourceDetectionService = new NewMockResourceUsageDetectionService()
            }));
            _generalExecutor = sys.ActorOf(GeneralExecutor.Props(sys, _serviceRouter), "exec");
            _generalExecutor.Tell(new RequestAddChainExecutor(chain.Id));
            ExpectMsg<RespondAddChainExecutor>();

            IParallelTransactionExecutingService parallelTransactionExecutingService =
                new ParallelTransactionExecutingService(sys);
            var synchronizer = new Kernel.Miner.BlockExecutor(poolService, parallelTransactionExecutingService,
                _chainManager, _blockManager);
            var res = await synchronizer.ExecuteBlock(block);
            Assert.True(res);
            Assert.Equal((ulong)2, await _chainManager.GetChainCurrentHeight(chain.Id));
            Assert.Equal(block.GetHash(), await _chainManager.GetChainLastBlockHash(chain.Id));
        }
    }
}