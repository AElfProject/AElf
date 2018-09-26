using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using AElf.ChainController;
using AElf.ChainController.TxMemPool;
using AElf.Configuration;
using AElf.Configuration.Config.GRPC;
using AElf.Cryptography.Certificate;
using AElf.Cryptography.ECDSA;
using AElf.Kernel;
using AElf.Kernel.Managers;
using AElf.Kernel.Storages;
using AElf.Miner.Miner;
using AElf.Miner.Rpc.Client;
using AElf.Miner.Rpc.Server;
using AElf.Runtime.CSharp;
using AElf.SmartContract;
using AElf.SmartContract.Metadata;
using Akka.Actor;
using Google.Protobuf;
using Google.Protobuf.Collections;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Moq;
using NLog;

namespace AElf.Miner.Tests
{
    public class MockSetup
    {
        private List<IBlockHeader> _headers = new List<IBlockHeader>();
        private List<IBlockHeader> _sideChainHeaders = new List<IBlockHeader>();
        private List<IBlock> _blocks = new List<IBlock>();
        private readonly ILogger _logger;
        private ulong _i;
        private IChainCreationService _chainCreationService;
        private IStateDictator _stateDictator;
        private ISmartContractManager _smartContractManager;
        private ISmartContractRunnerFactory _smartContractRunnerFactory;
        private IAccountContextService _accountContextService;
        private ITransactionManager _transactionManager;
        private ITransactionResultManager _transactionResultManager;
        private IExecutingService _concurrencyExecutingService;
        private IFunctionMetadataService _functionMetadataService;
        private IChainService _chainService;
        private IBinaryMerkleTreeManager _binaryMerkleTreeManager;
        private readonly IDataStore _dataStore;
        
        public MockSetup(ILogger logger, IDataStore dataStore)
        {
            _logger = logger;
            _dataStore = dataStore;
            Initialize();
        }
        
        private void Initialize()
        {
            _transactionManager = new TransactionManager(_dataStore, _logger);
            _stateDictator = new StateDictator(new HashManager(_dataStore), _transactionManager, _dataStore, _logger);
            _smartContractManager = new SmartContractManager(_dataStore);
            _accountContextService = new AccountContextService(_stateDictator);
            _transactionResultManager = new TransactionResultManager(_dataStore);
            _functionMetadataService = new FunctionMetadataService(_dataStore, _logger);
            _chainService = new ChainService(new ChainManagerBasic(_dataStore),
                new BlockManagerBasic(_dataStore, _logger),
                _transactionManager, _dataStore);
            _smartContractRunnerFactory = new SmartContractRunnerFactory();
            var runner = new SmartContractRunner("../../../../AElf.SDK.CSharp/bin/Debug/netstandard2.0/");
            _smartContractRunnerFactory.AddRunner(0, runner);
            
            _concurrencyExecutingService = new SimpleExecutingService(
                new SmartContractService(_smartContractManager, _smartContractRunnerFactory, _stateDictator,
                    _functionMetadataService), _stateDictator, new ChainContextService(_chainService));
            
            _chainCreationService = new ChainCreationService(_chainService,
                new SmartContractService(new SmartContractManager(_dataStore), _smartContractRunnerFactory,
                    _stateDictator, _functionMetadataService), _logger);

            _binaryMerkleTreeManager = new BinaryMerkleTreeManager(_dataStore);
        }

        private byte[] SmartContractZeroCode => ContractCodes.TestContractZeroCode;


        public async Task<IChain> CreateChain()
        {            var chainId = Hash.Generate();
            var reg = new SmartContractRegistration
            {
                Category = 0,
                ContractBytes = ByteString.CopyFrom(SmartContractZeroCode),
                ContractHash = SmartContractZeroCode.CalculateHash()
            };

            var chain = await _chainCreationService.CreateNewChainAsync(chainId,
                new List<SmartContractRegistration> {reg});
            _stateDictator.ChainId = chainId;
            return chain;
        }
        
        internal IMiner GetMiner(IMinerConfig config, TxPoolService poolService, ClientManager clientManager = null)
        {
            var miner = new AElf.Miner.Miner.Miner(config, poolService, _chainService, _stateDictator,
                _concurrencyExecutingService, _transactionManager, _transactionResultManager, _logger,
                clientManager, _binaryMerkleTreeManager);

            return miner;
        }
        
        internal IBlockExecutor GetBlockExecutor(TxPoolService poolService, ClientManager clientManager = null)
        {
            var blockExecutor = new BlockExecutor(poolService, _chainService, _stateDictator,
                _concurrencyExecutingService, _logger, _transactionManager, _transactionResultManager,
                clientManager);

            return blockExecutor;
        }

        internal IBlockChain GetBlockChain(Hash chainId)
        {
            return _chainService.GetBlockChain(chainId);
        }
        
        internal TxPoolService CreateTxPoolService(Hash chainId)
        {
            var poolconfig = TxPoolConfig.Default;
            poolconfig.ChainId = chainId;
            var contract = new ContractTxPool(poolconfig, _logger);
            var prior = new PriorTxPool(poolconfig, _logger);
            return new TxPoolService(contract, _accountContextService, _logger, prior);
        }

        public IMinerConfig GetMinerConfig(Hash chainId, ulong txCountLimit, byte[] getAddress)
        {
            return new MinerConfig
            {
                ChainId = chainId,
                CoinBase = getAddress
            };
        }

        private Mock<ILightChain> MockLightChain()
        {
            Mock<ILightChain> mock = new Mock<ILightChain>();
            mock.Setup(lc => lc.GetCurrentBlockHeightAsync()).Returns(Task.FromResult((ulong)_headers.Count - 1));
            mock.Setup(lc => lc.GetHeaderByHeightAsync(It.IsAny<ulong>()))
                .Returns<ulong>(p => Task.FromResult(_sideChainHeaders[(int) p]));

            return mock;
        }

        private Mock<IBlockChain> MockBlockChain()
        {
            Mock<IBlockChain> mock = new Mock<IBlockChain>();
            mock.Setup(bc => bc.GetBlockByHeightAsync(It.IsAny<ulong>()))
                .Returns<ulong>(p => Task.FromResult(_blocks[(int) p]));
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
                SideChainBlockHeadersRoot = Hash.Generate(),
                ChainId = Hash.Generate(),
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
                ChainId = chainId ?? Hash.Generate(),
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

        private Mock<IBinaryMerkleTreeManager> MockBinaryMerkleTreeManager()
        {
            Mock<IBinaryMerkleTreeManager> mock = new Mock<IBinaryMerkleTreeManager>();
            mock.Setup(b => b.GetSideChainTransactionRootsMerkleTreeByHeightAsync(It.IsAny<Hash>(), It.IsAny<ulong>()))
                .Returns<Hash, ulong>((_, u) =>
                {
                    _blocks[(int) u].Body.CalculateMerkleTreeRoots();
                    return Task.FromResult(_blocks[(int) u].Body.BinaryMerkleTreeForSideChainTransactionRoots);
                });
            return mock;
        }
        
        public ParentChainBlockInfoRpcServerImpl MockParentChainBlockInfoRpcServerImpl()
        {
            return new ParentChainBlockInfoRpcServerImpl(MockChainService().Object, _logger, MockBinaryMerkleTreeManager().Object);
        }

        public SideChainBlockInfoRpcServerImpl MockSideChainBlockInfoRpcServerImpl()
        {
            return new SideChainBlockInfoRpcServerImpl(MockChainService().Object, _logger);
        }
        
        public ServerManager ServerManager(ParentChainBlockInfoRpcServerImpl impl1, SideChainBlockInfoRpcServerImpl impl2)
        {
            return new ServerManager(impl1, impl2);
        }
        
        public Mock<IChainManagerBasic> MockChainManager()
        {
            var mock = new Mock<IChainManagerBasic>();
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
            return new ClientManager(_logger, MockChainManager().Object);
        }

        public void MockKeyPair(Hash chainId, string dir)
        {
            
            var certificateStore = new CertificateStore(dir);
            var name = chainId.ToHex();
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
            
            var sideChainId = Hash.Generate();
            NodeConfig.Instance.ChainId = sideChainId.ToHex();
            
            MockKeyPair(sideChainId, dir);
            GrpcLocalConfig.Instance.LocalSideChainServerPort = port;
            GrpcLocalConfig.Instance.LocalServerIP = address;
            GrpcLocalConfig.Instance.SideChainServer = true;
            //start server, sidechain is server-side
            
            return sideChainId;
        }

        public Hash MockParentChainServer(int port, string address, string dir, Hash chainId = null)
        {
            
            chainId = chainId??Hash.Generate();
            
            _headers = new List<IBlockHeader>
            {
                MockBlockHeader(),
                MockBlockHeader(),
                MockBlockHeader()
            };
            //IBlockHeader blockHeader = Headers[0];
            _blocks = new List<IBlock>
            {
                MockBlock(_headers[0], MockBlockBody(0, chainId)).Object,
                MockBlock(_headers[1], MockBlockBody(1, chainId)).Object,
                MockBlock(_headers[2], MockBlockBody(2, chainId)).Object
            };

            MockKeyPair(chainId, dir);
            GrpcLocalConfig.Instance.LocalParentChainServerPort = port;
            GrpcLocalConfig.Instance.LocalServerIP = address;
            GrpcLocalConfig.Instance.ParentChainServer = true;
            NodeConfig.Instance.ChainId = chainId.ToHex();
            
            return chainId;
        }
        
        
        public void ClearDirectory(string dir)
        {
            if(Directory.Exists(Path.Combine(dir, "certs")))
                Directory.Delete(Path.Combine(dir, "certs"), true);
        }
    }
}