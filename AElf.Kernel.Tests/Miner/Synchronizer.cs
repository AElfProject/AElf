using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using AElf.Cryptography.ECDSA;
using AElf.ChainController;
using AElf.SmartContract.Metadata;
using AElf.Execution;
using AElf.Execution.Scheduling;
using AElf.Kernel.Managers;
using AElf.Kernel.Storages;
using AElf.Kernel.Tests.Concurrency.Scheduling;
using AElf.Kernel.TxMemPool;
using AElf.Runtime.CSharp;
using AElf.Types.CSharp;
using AElf.SmartContract;
using Akka.Actor;
using Google.Protobuf;
using ServiceStack;
using Xunit;
using Xunit.Frameworks.Autofac;
using Akka.TestKit;
using Akka.TestKit.Xunit;
using Google.Protobuf.WellKnownTypes;
using NLog;

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

        public Synchronizer(
            IChainCreationService chainCreationService, IChainContextService chainContextService,
            IChainManager chainManager, IBlockManager blockManager, ILogger logger,
            ITransactionResultManager transactionResultManager, ITransactionManager transactionManager,
            FunctionMetadataService functionMetadataService, IConcurrencyExecutingService concurrencyExecutingService,
            IChangesStore changesStore, IWorldStateStore worldStateStore, IDataStore dataStore,
            ISmartContractManager smartContractManager, IAccountContextService accountContextService) : base(new XunitAssertions())
        {

            _chainCreationService = chainCreationService;
            _chainContextService = chainContextService;
            _chainManager = chainManager;
            _blockManager = blockManager;
            _logger = logger;
            _transactionResultManager = transactionResultManager;
            _transactionManager = transactionManager;
            _functionMetadataService = functionMetadataService;
            _concurrencyExecutingService = concurrencyExecutingService;
            _worldStateDictator =
                new WorldStateDictator(worldStateStore, changesStore, dataStore, _logger, _transactionManager);
            _smartContractManager = smartContractManager;
            _accountContextService = accountContextService;

            Initialize();
        }

        private void Initialize()
        {
            _smartContractRunnerFactory = new SmartContractRunnerFactory();
            var runner = new SmartContractRunner("../../../../AElf.SDK.CSharp/bin/Debug/netstandard2.0/");
            _smartContractRunnerFactory.AddRunner(0, runner);
            _smartContractService = new SmartContractService(_smartContractManager, _smartContractRunnerFactory, _worldStateDictator, _functionMetadataService);

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
        
        public byte[] OldSmartContractZeroCode
        {
            get
            {
                const string TestContractZeroName = "AElf.Kernel.Tests.TestContractZero";
                byte[] code = null;
                using (FileStream file = File.OpenRead(System.IO.Path.GetFullPath($"../../../../{TestContractZeroName}/bin/Debug/netstandard2.0/{TestContractZeroName}.dll")))
                {
                    code = file.ReadFully();
                }
                return code;
            }
        }
        
        
        public byte[] NewSmartContractZeroCode
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
        
        public async Task<IChain> CreateChain(byte[] SmartContractZeroCode)
        {
            var chainId = Hash.Generate();
            var reg = new SmartContractRegistration
            {
                Category = 0,
                ContractBytes = ByteString.CopyFrom(SmartContractZeroCode),
                ContractHash = SmartContractZeroCode.CalculateHash()
            };

            var chain = await _chainCreationService.CreateNewChainAsync(chainId, reg);
            _worldStateDictator.SetChainId(chainId);
            return chain;
        }


        public List<Transaction> CreateTxs(Hash chainId)
        {
            var contractAddressZero = new Hash(chainId.CalculateHashWith(Globals.SmartContractZeroIdString)).ToAccount();

            var code = ExampleContractCode;
            
            ECKeyPair keyPair = new KeyPairGenerator().Generate();
            ECSigner signer = new ECSigner();
            var txnDep = new Transaction()
            {
                From = keyPair.GetAddress(),
                To = contractAddressZero,
                IncrementId = 0,
                MethodName = "DeploySmartContract",
                Params = ByteString.CopyFrom(ParamsPacker.Pack((int)0, code)),
                
                Fee = TxPoolConfig.Default.FeeThreshold + 1
            };
            
            Hash hash = txnDep.GetHash();

            ECSignature signature1 = signer.Sign(keyPair, hash.GetHashBytes());
            txnDep.P = ByteString.CopyFrom(keyPair.PublicKey.Q.GetEncoded());
            txnDep.R = ByteString.CopyFrom(signature1.R); 
            txnDep.S = ByteString.CopyFrom(signature1.S);
            
            var txInv_1 = new Transaction
            {
                From = keyPair.GetAddress(),
                To = contractAddressZero,
                IncrementId = 1,
                MethodName = "Print",
                Params = ByteString.CopyFrom(ParamsPacker.Pack("AElf")),
                
                Fee = TxPoolConfig.Default.FeeThreshold + 1
            };
            ECSignature signature2 = signer.Sign(keyPair, txInv_1.GetHash().GetHashBytes());
            txInv_1.P = ByteString.CopyFrom(keyPair.PublicKey.Q.GetEncoded());
            txInv_1.R = ByteString.CopyFrom(signature2.R); 
            txInv_1.S = ByteString.CopyFrom(signature2.S);
            
            var txInv_2 = new Transaction
            {
                From = keyPair.GetAddress(),
                To = contractAddressZero,
                IncrementId =txInv_1.IncrementId,
                MethodName = "Print",
                Params = ByteString.CopyFrom(ParamsPacker.Pack("Hoopox")),
                
                Fee = TxPoolConfig.Default.FeeThreshold + 1
            };
            
            ECSignature signature3 = signer.Sign(keyPair, txInv_2.GetHash().GetHashBytes());
            txInv_2.P = ByteString.CopyFrom(keyPair.PublicKey.Q.GetEncoded());
            txInv_2.R = ByteString.CopyFrom(signature3.R); 
            txInv_2.S = ByteString.CopyFrom(signature3.S);
            
            var txs = new List<Transaction>(){
                txnDep, txInv_1, txInv_2
            };

            return txs;
        }
        
        private IAccountContextService _accountContextService;
        private readonly ILogger _logger;
        private readonly ITransactionManager _transactionManager;
        private readonly ITransactionResultManager _transactionResultManager;
        private ISmartContractRunnerFactory _smartContractRunnerFactory;
        private ISmartContractService _smartContractService;
        private IActorRef _requestor;

        
        [Fact]
        public async Task SyncGenesisBlock_False_Rollback()
        {
            var poolconfig = TxPoolConfig.Default;
            var chain = await CreateChain(NewSmartContractZeroCode);
            poolconfig.ChainId = chain.Id;
            
            var pool = new TxPool(poolconfig, _logger);
            
            var poolService = new TxPoolService(pool, _accountContextService, _logger);
            
            poolService.Start();
            var block = GenerateBlock(chain.Id, chain.GenesisBlockHash, 1);
            
            var txs = CreateTxs(chain.Id);
            foreach (var transaction in txs)
            {
                await poolService.AddTxAsync(transaction);
            }
            
            Assert.Equal((ulong)0, await poolService.GetWaitingSizeAsync());
            Assert.Equal((ulong)2, await poolService.GetExecutableSizeAsync());
            Assert.True(poolService.TryGetTx(txs[2].GetHash(), out var tx));
            
            block.Body.Transactions.Add(txs[0].GetHash());
            block.Body.Transactions.Add(txs[2].GetHash());

            block.FillTxsMerkleTreeRootInHeader();
            block.Body.BlockHeader = block.Header.GetHash();

            var synchronizer = new BlockExecutor(poolService,
                _chainManager, _blockManager, _worldStateDictator, _concurrencyExecutingService, null, _transactionManager, _transactionResultManager);

            synchronizer.Start(new Grouper(_servicePack.ResourceDetectionService));
            var res = await synchronizer.ExecuteBlock(block);
            Assert.False(res);

            Assert.Equal((ulong)0, await poolService.GetWaitingSizeAsync());
            Assert.Equal((ulong)2, await poolService.GetExecutableSizeAsync());
            //Assert.False(poolService.TryGetTx(txs[2].GetHash(), out tx));
            Assert.True(poolService.TryGetTx(txs[1].GetHash(), out tx));
            Assert.Equal((ulong)0, pool.GetNonce(tx.From));

            Assert.Equal((ulong)1, await _chainManager.GetChainCurrentHeight(chain.Id));
            Assert.Equal(chain.GenesisBlockHash, await _chainManager.GetChainLastBlockHash(chain.Id));
        }
    }
}