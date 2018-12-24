using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using AElf.ChainController;
using AElf.ChainController.CrossChain;
using AElf.Configuration;
using AElf.Configuration.Config.GRPC;
using AElf.Cryptography.Certificate;
using AElf.Kernel;
using AElf.Kernel.Managers;
using AElf.Kernel.Storages;
using AElf.Miner.Miner;
using AElf.Miner.Rpc.Server;
using AElf.Runtime.CSharp;
using AElf.SmartContract;
using AElf.SmartContract.Metadata;
using Google.Protobuf;
using Moq;
using AElf.Common;
using AElf.Configuration.Config.Chain;
using AElf.Database;
using AElf.Execution.Execution;
using AElf.Kernel.Types.Transaction;
using AElf.Miner.Rpc.Client;
using AElf.Miner.TxMemPool;
using AElf.SmartContract.Proposal;
using AElf.Synchronization.BlockExecution;
using AElf.Synchronization.BlockSynchronization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using ServiceStack;
using Volo.Abp.DependencyInjection;

namespace AElf.Miner.Tests
{
    public class MockSetup : ITransientDependency
    {
        private List<IBlockHeader> _headers = new List<IBlockHeader>();
        private List<IBlockHeader> _sideChainHeaders = new List<IBlockHeader>();
        private List<IBlock> _blocks = new List<IBlock>();
        public ILogger<MockSetup> Logger {get;set;}
        private ulong _i = 0;
        private IChainCreationService _chainCreationService;
        private ISmartContractManager _smartContractManager;
        private ISmartContractRunnerContainer _smartContractRunnerContainer;
        private ITransactionManager _transactionManager;
        private ITransactionReceiptManager _transactionReceiptManager;
        private ITransactionResultManager _transactionResultManager;
        private ITransactionTraceManager _transactionTraceManager;
        private IExecutingService _concurrencyExecutingService;
        private IFunctionMetadataService _functionMetadataService;
        private IChainService _chainService;
        private IBinaryMerkleTreeManager _binaryMerkleTreeManager;
        private IKeyValueDatabase _database;
        private readonly IDataStore _dataStore;
        public readonly IStateStore StateStore;
        private IChainContextService _chainContextService;
        private ITxSignatureVerifier _signatureVerifier;
        private ITxRefBlockValidator _refBlockValidator;
        private IChainManager _chainManager;
        private IAuthorizationInfoReader _authorizationInfoReader;
        private IStateStore _stateStore;

        public MockSetup( IKeyValueDatabase database, IDataStore dataStore, IStateStore stateStore, ITxSignatureVerifier signatureVerifier, ITxRefBlockValidator refBlockValidator)
        {
            Logger = NullLogger<MockSetup>.Instance;
            _database = database;
            _dataStore = dataStore;
            StateStore = stateStore;
            _signatureVerifier = signatureVerifier;
            _refBlockValidator = refBlockValidator;
            Initialize();
        }

        private void Initialize()
        {
            _transactionManager = new TransactionManager(_dataStore);
            _transactionReceiptManager = new TransactionReceiptManager(_database);
            _smartContractManager = new SmartContractManager(_dataStore);
            _transactionResultManager = new TransactionResultManager(_dataStore);
            _transactionTraceManager = new TransactionTraceManager(_dataStore);
            _functionMetadataService = new FunctionMetadataService(_dataStore);
            _chainManager = new ChainManager(_dataStore);
            _chainService = new ChainService(_chainManager, new BlockManager(_dataStore),
                _transactionManager, _transactionTraceManager, _dataStore, StateStore);
            _smartContractRunnerContainer = new SmartContractRunnerContainer();
            /*var runner = new SmartContractRunner("../../../../AElf.SDK.CSharp/bin/Debug/netstandard2.0/");
            _smartContractRunnerContainer.AddRunner(0, runner);*/
            var runner = new SmartContractRunner(ContractCodes.TestContractFolder);
            _smartContractRunnerContainer.AddRunner(0, runner);
            _concurrencyExecutingService = new SimpleExecutingService(
                new SmartContractService(_smartContractManager, _smartContractRunnerContainer, StateStore,
                    _functionMetadataService), _transactionTraceManager, StateStore,
                new ChainContextService(_chainService));

            _chainCreationService = new ChainCreationService(_chainService,
                new SmartContractService(_smartContractManager, _smartContractRunnerContainer,
                    StateStore, _functionMetadataService));

            _binaryMerkleTreeManager = new BinaryMerkleTreeManager(_dataStore);
            _chainContextService = new ChainContextService(_chainService);
            _authorizationInfoReader = new AuthorizationInfoReader(StateStore);
            _stateStore = new StateStore(_database);
        }

        private byte[] SmartContractZeroCode => ContractCodes.TestContractZeroCode;

        public byte[] CrossChainCode
        {
            get
            {
                byte[] code = null;
                using (FileStream file = File.OpenRead(Path.GetFullPath("../../../../AElf.Contracts.CrossChain/bin/Debug/netstandard2.0/AElf.Contracts.CrossChain.dll")))
                {
                    code = file.ReadFully();
                }
                return code;
            }
        }
        public async Task<IChain> CreateChain()
        {            
            var chainId = Hash.LoadByteArray(ChainHelpers.GetRandomChainId());
            
            var reg = new SmartContractRegistration
            {
                Category = 0,
                ContractBytes = ByteString.CopyFrom(SmartContractZeroCode),
                ContractHash = Hash.FromRawBytes(SmartContractZeroCode)
            };
            var reg1 = new SmartContractRegistration
            {
                Category = 0,
                ContractBytes = ByteString.CopyFrom(CrossChainCode),
                ContractHash = Hash.FromRawBytes(CrossChainCode),
                SerialNumber = GlobalConfig.CrossChainContract
            };

            var chain = await _chainCreationService.CreateNewChainAsync(chainId,
                new List<SmartContractRegistration> {reg, reg1});
            return chain;
        }

        internal IMiner GetMiner(IMinerConfig config, ITxHub hub, ClientManager clientManager = null)
        {
            var miner = new AElf.Miner.Miner.Miner(config, hub, _chainService, _concurrencyExecutingService,
                _transactionResultManager, clientManager, _binaryMerkleTreeManager, null,
                MockBlockValidationService().Object, _chainContextService, _stateStore);

            return miner;
        }

        internal IBlockExecutor GetBlockExecutor(ClientManager clientManager = null)
        {
            var blockExecutor = new BlockExecutor(_chainService, _concurrencyExecutingService,
                _transactionResultManager, clientManager, _binaryMerkleTreeManager,

                new TxHub(_transactionManager, _transactionReceiptManager, _chainService, _authorizationInfoReader, _signatureVerifier, _refBlockValidator), StateStore);


            return blockExecutor;
        }

        internal IBlockChain GetBlockChain(Hash chainId)
        {
            return _chainService.GetBlockChain(chainId);
        }
        
        internal ITxHub CreateAndInitTxHub()
        {
            var hub = new TxHub(_transactionManager, _transactionReceiptManager, _chainService, _authorizationInfoReader, _signatureVerifier, _refBlockValidator);
            hub.Initialize();
            return hub;
        }

        public IMinerConfig GetMinerConfig(Hash chainId)
        {
            return new MinerConfig { ChainId = chainId };
        }

        private Mock<ILightChain> MockLightChain()
        {
            Mock<ILightChain> mock = new Mock<ILightChain>();
            mock.Setup(lc => lc.GetCurrentBlockHeightAsync()).Returns(Task.FromResult((ulong)_headers.Count - 1 + GlobalConfig.GenesisBlockHeight));
            mock.Setup(lc => lc.GetHeaderByHeightAsync(It.IsAny<ulong>()))
                .Returns<ulong>(p =>
                {
                    return (int)p > _sideChainHeaders.Count ? null :Task.FromResult(_sideChainHeaders[(int) p - 1]);
                });

            return mock;
        }

        private Mock<IBlockChain> MockBlockChain()
        {
            Mock<IBlockChain> mock = new Mock<IBlockChain>();
            mock.Setup(bc => bc.GetBlockByHeightAsync(It.IsAny<ulong>()))
                .Returns<ulong>(p => Task.FromResult(_blocks[(int) p - 1]));
            return mock;
        }

        private Mock<IChainService> MockChainService()
        {
            Mock<IChainService> mock = new Mock<IChainService>();
            mock.Setup(cs => cs.GetLightChain(It.IsAny<Hash>())).Returns(MockLightChain().Object);
            mock.Setup(cs => cs.GetBlockChain(It.IsAny<Hash>())).Returns(MockBlockChain().Object);
            return mock;
        }

        private IBlockHeader MockBlockHeader()
        {
            return new BlockHeader
            {
                MerkleTreeRootOfTransactions = Hash.Generate(),
                SideChainTransactionsRoot = Hash.Generate(),
                ChainId = Hash.LoadByteArray(ChainHelpers.GetRandomChainId()),
                PreviousBlockHash = Hash.Generate(),
                MerkleTreeRootOfWorldState = Hash.Generate()
            };
        }

        private IBlockBody MockBlockBody(ulong height, Hash chainId = null)
        {
            return new BlockBody
            {
                IndexedInfo = { MockSideChainBlockInfo(height, chainId)}
            };
        }

        private SideChainBlockInfo MockSideChainBlockInfo(ulong height, Hash chainId = null)
        {
            return new SideChainBlockInfo
            {
                Height = height,
                ChainId = chainId ?? Hash.LoadByteArray(ChainHelpers.GetRandomChainId()),
                TransactionMKRoot = Hash.Generate(),
                BlockHeaderHash = Hash.Generate()
            };
        }
        
        public Mock<IBlock> MockBlock(IBlockHeader header, IBlockBody body)
        {
            Mock<IBlock> mock = new Mock<IBlock>();
            mock.Setup(b => b.Header).Returns((BlockHeader)header);
            mock.Setup(b => b.Body).Returns((BlockBody)body);
            return mock;
        }
        
        public ParentChainBlockInfoRpcServer MockParentChainBlockInfoRpcServer()
        {
            return new ParentChainBlockInfoRpcServer(MockChainService().Object, MockCrossChainInfoReader().Object);
        }

        public SideChainBlockInfoRpcServer MockSideChainBlockInfoRpcServer()
        {
            return new SideChainBlockInfoRpcServer(MockChainService().Object);
        }
        
        public ServerManager ServerManager(ParentChainBlockInfoRpcServer impl1, SideChainBlockInfoRpcServer impl2)
        {
            return new ServerManager(impl1, impl2);
        }
        
        public Mock<IChainManager> MockChainManager()
        {
            var mock = new Mock<IChainManager>();
            mock.Setup(cm => cm.GetCurrentBlockHeightAsync(It.IsAny<Hash>())).Returns(() =>
            {
                var k = _i;
                return Task.FromResult(k);
            });
            mock.Setup(cm => cm.UpdateCurrentBlockHeightAsync(It.IsAny<Hash>(), It.IsAny<ulong>()))
                .Returns<Hash, ulong>((h, u) =>
                {
                    _i = u;
                    return Task.CompletedTask;
                });
            return mock;
        }

        public ClientManager MinerClientManager()
        {
            return new ClientManager(MockCrossChainInfoReader().Object);
        }

        public ulong GetTimes = 0;
        private Mock<ICrossChainInfoReader> MockCrossChainInfoReader()
        {
            var mock = new Mock<ICrossChainInfoReader>();
            mock.Setup(m => m.GetParentChainCurrentHeight()).Returns(() => GetTimes);
            mock.Setup(m => m.GetMerkleTreeForSideChainTransactionRoot(It.IsAny<ulong>())).Returns<ulong>(u =>
            {
                var binaryMerkleTree = new BinaryMerkleTree();
                binaryMerkleTree.AddNodes(_blocks[(int) u - 1].Body.IndexedInfo.Select(info => info.TransactionMKRoot));
                Console.WriteLine($"merkle tree root for {u} : {binaryMerkleTree.ComputeRootHash()}");
                return binaryMerkleTree;
            });
            mock.Setup(m => m.GetSideChainCurrentHeight(It.IsAny<Hash>())).Returns<Hash>(chainId => GetTimes);
            return mock;
        }

        public void MockKeyPair(Hash chainId, string dir)
        {
            
            var certificateStore = new CertificateStore(dir);
            var name = chainId.DumpBase58();
            var keyPair = certificateStore.WriteKeyAndCertificate(name, "127.0.0.1");
        }
        
        public Hash MockSideChainServer(int port, string address, string dir)
        {
            _sideChainHeaders = new List<IBlockHeader>
            {
                MockBlockHeader(),
                MockBlockHeader(),
                MockBlockHeader()
            };
            
            var sideChainId = Hash.LoadByteArray(ChainHelpers.GetRandomChainId());
            ChainConfig.Instance.ChainId = sideChainId.DumpBase58();
            
            MockKeyPair(sideChainId, dir);
            GrpcLocalConfig.Instance.LocalSideChainServerPort = port;
            GrpcLocalConfig.Instance.LocalServerIP = address;
            GrpcLocalConfig.Instance.SideChainServer = true;
            //start server, sidechain is server-side
            
            return sideChainId;
        }

        public Hash MockParentChainServer(int port, string address, string dir, Hash chainId = null)
        {
            
            chainId = chainId??Hash.LoadByteArray(ChainHelpers.GetRandomChainId());
            
            _headers = new List<IBlockHeader>
            {
                MockBlockHeader(),
                MockBlockHeader(),
                MockBlockHeader()
            };
            //IBlockHeader blockHeader = Headers[0];
            _blocks = new List<IBlock>
            {
                MockBlock(_headers[0], MockBlockBody(GlobalConfig.GenesisBlockHeight, chainId)).Object,
                MockBlock(_headers[1], MockBlockBody(GlobalConfig.GenesisBlockHeight + 1, chainId)).Object,
                MockBlock(_headers[2], MockBlockBody(GlobalConfig.GenesisBlockHeight + 2, chainId)).Object
            };

            MockKeyPair(chainId, dir);
            GrpcLocalConfig.Instance.LocalParentChainServerPort = port;
            GrpcLocalConfig.Instance.LocalServerIP = address;
            GrpcLocalConfig.Instance.ParentChainServer = true;
            ChainConfig.Instance.ChainId = chainId.DumpBase58();
            
            return chainId;
        }

        private Mock<IBlockValidationService> MockBlockValidationService()
        {
            var mock = new Mock<IBlockValidationService>();
            mock.Setup(bvs => bvs.ValidateBlockAsync(It.IsAny<IBlock>(), It.IsAny<IChainContext>()))
                .Returns(() => Task.FromResult(BlockValidationResult.Success));
            return mock;
        }
        
        public void ClearDirectory(string dir)
        {
            if(Directory.Exists(Path.Combine(dir, "certs")))
                Directory.Delete(Path.Combine(dir, "certs"), true);
        }
    }
}