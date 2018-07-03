using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using AElf.Cryptography.ECDSA;
using AElf.Kernel.Concurrency;
using AElf.Kernel.Concurrency.Execution;
using AElf.Kernel.Concurrency.Execution.Messages;
using AElf.Kernel.Concurrency.Scheduling;
using AElf.Kernel.Concurrency.Metadata;
using AElf.Kernel.KernelAccount;
using AElf.Kernel.Managers;
using AElf.Kernel.Services;
using AElf.Kernel.Storages;
using AElf.Kernel.Tests.Concurrency.Execution;
using AElf.Kernel.Tests.Concurrency.Scheduling;
using AElf.Kernel.TxMemPool;
using AElf.Runtime.CSharp;
using AElf.Types.CSharp;
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
        private IChainContextService _chainContextService;
        private IWorldStateDictator _worldStateDictator;
        private ISmartContractManager _smartContractManager;
        private IFunctionMetadataService _functionMetadataService;
        private IConcurrencyExecutingService _concurrencyExecutingService;

        private ServicePack _servicePack;
        private IActorRef _requestor;

        public Synchronizer(IWorldStateDictator worldStateDictator, ISmartContractStore smartContractStore,
            IChainCreationService chainCreationService, IChainContextService chainContextService, IChainManager chainManager, IBlockManager blockManager, ILogger logger, ITransactionResultManager transactionResultManager, ITransactionManager transactionManager, IAccountContextService accountContextService, IFunctionMetadataService functionMetadataService, ISmartContractRunnerFactory smartContractRunnerFactory, IConcurrencyExecutingService concurrencyExecutingService) : base(new XunitAssertions())
        {
            _chainCreationService = chainCreationService;
            _chainContextService = chainContextService;
            _chainManager = chainManager;
            _blockManager = blockManager;
            _logger = logger;
            _transactionResultManager = transactionResultManager;
            _transactionManager = transactionManager;
            _accountContextService = accountContextService;
            _functionMetadataService = functionMetadataService;
            _smartContractRunnerFactory = smartContractRunnerFactory;
            _concurrencyExecutingService = concurrencyExecutingService;

            _worldStateDictator = worldStateDictator;
            _smartContractManager = new SmartContractManager(smartContractStore);
        }

        private async Task Initialize(Hash chainId)
        {
            _smartContractRunnerFactory = new SmartContractRunnerFactory();
            var runner = new SmartContractRunner("../../../../AElf.SDK.CSharp/bin/Debug/netstandard2.0/");
            _smartContractRunnerFactory.AddRunner(0, runner);
            _smartContractService = new SmartContractService(_smartContractManager, _smartContractRunnerFactory, _worldStateDictator.SetChainId(chainId), _functionMetadataService);

            _servicePack = new ServicePack
            {
                ChainContextService = _chainContextService,
                SmartContractService = _smartContractService,
                ResourceDetectionService = new NewMockResourceUsageDetectionService(),
                WorldStateDictator = _worldStateDictator
            };
            
            var workers = new[] {"/user/worker1", "/user/worker2"};
            var worker1 = Sys.ActorOf(Props.Create<Worker>(), "worker1");
            var worker2 = Sys.ActorOf(Props.Create<Worker>(), "worker2");
            var router = Sys.ActorOf(Props.Empty.WithRouter(new TrackedGroup(workers)), "router");
            worker1.Tell(new LocalSerivcePack(_servicePack));
            worker2.Tell(new LocalSerivcePack(_servicePack));
            _requestor = Sys.ActorOf(Requestor.Props(router));
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
                    Time = Timestamp.FromDateTime(DateTime.UtcNow),
                    MerkleTreeRootOfWorldState = Hash.Default
                }
            };
            block.FillTxsMerkleTreeRootInHeader();
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

            await Initialize(chainId);
            
            return chain;
        }


        public List<ITransaction> CreateTxs(Hash chainId)
        {
            var contractAddressZero = new Hash(chainId.CalculateHashWith(Globals.SmartContractZeroIdString)).ToAccount();

            var code = ExampleContractCode;
            
            ECKeyPair keyPair = new KeyPairGenerator().Generate();
            ECSigner signer = new ECSigner();
            var txnDep = new Transaction()
            {
                From = keyPair.GetAddress(),
                To = contractAddressZero,
                IncrementId = NewIncrementId(),
                MethodName = "DeploySmartContract",
                Params = ByteString.CopyFrom(ParamsPacker.Pack((int)0, code)),
                
                Fee = TxPoolConfig.Default.FeeThreshold + 1
            };
            
            Hash hash = txnDep.GetHash();

            ECSignature signature = signer.Sign(keyPair, hash.GetBytes());
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
        private ISmartContractRunnerFactory _smartContractRunnerFactory;
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
            block.Body.BlockHeader = block.Header.GetHash();


            /*IParallelTransactionExecutingService parallelTransactionExecutingService =
                new ParallelTransactionExecutingService(_requestor,
                    new Grouper(_servicePack.ResourceDetectionService));*/
            var synchronizer = new Kernel.Miner.BlockExecutor(poolService,
                _chainManager, _blockManager, _worldStateDictator, _concurrencyExecutingService, null);
            synchronizer.Start(new Grouper(_servicePack.ResourceDetectionService));
            var res = await synchronizer.ExecuteBlock(block);
            Assert.True(res);
            Assert.Equal((ulong)2, await _chainManager.GetChainCurrentHeight(chain.Id));
            Assert.Equal(block.GetHash(), await _chainManager.GetChainLastBlockHash(chain.Id));
        }
    }
}