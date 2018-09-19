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
using AElf.Miner.Miner;
using AElf.Miner.Rpc.Client;
using AElf.Miner.Rpc.Server;
using AElf.Runtime.CSharp;
using AElf.SmartContract;
using Akka.Actor;
using Google.Protobuf;
using Google.Protobuf.Collections;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Moq;
using NLog;

namespace AElf.Miner.Tests.Grpc
{
    public class MockSetup
    {
        public List<IBlockHeader> Headers = new List<IBlockHeader>();
        public List<IBlockHeader> SideChainHeaders = new List<IBlockHeader>();
        public List<IBlock> Blocks = new List<IBlock>();
        private readonly ILogger _logger;
        private ulong _i;
        private readonly IChainCreationService _chainCreationService;
        private readonly IStateDictator _stateDictator;
        private readonly ISmartContractManager _smartContractManager;

        private ISmartContractRunnerFactory _smartContractRunnerFactory;
        private readonly IAccountContextService _accountContextService;
        private readonly ITransactionManager _transactionManager;
        private readonly ITransactionResultManager _transactionResultManager;
        private readonly IExecutingService _concurrencyExecutingService;
        private readonly IFunctionMetadataService _functionMetadataService;
        private readonly IChainService _chainService;
        
        public MockSetup(ILogger logger, IChainCreationService chainCreationService, IStateDictator stateDictator, 
            ISmartContractManager smartContractManager, IAccountContextService accountContextService, 
            ITransactionManager transactionManager, ITransactionResultManager transactionResultManager, 
            IExecutingService concurrencyExecutingService, IFunctionMetadataService functionMetadataService, 
            IChainService chainService)
        {
            _chainCreationService = chainCreationService;
            _stateDictator = stateDictator;
            _smartContractManager = smartContractManager;
            _accountContextService = accountContextService;
            _transactionManager = transactionManager;
            _transactionResultManager = transactionResultManager;
            _concurrencyExecutingService = concurrencyExecutingService;
            _functionMetadataService = functionMetadataService;
            _chainService = chainService;
            _logger = logger;
            Initialize();
        }
        
        private void Initialize()
        {
            _smartContractRunnerFactory = new SmartContractRunnerFactory();
            var runner = new SmartContractRunner("../../../../AElf.SDK.CSharp/bin/Debug/netstandard2.0/");
            _smartContractRunnerFactory.AddRunner(0, runner);
            new SmartContractService(_smartContractManager, _smartContractRunnerFactory, _stateDictator, _functionMetadataService);
        }
        
        public byte[] SmartContractZeroCode => ContractCodes.TestContractZeroCode;


        public async Task<IChain> CreateChain()
        {            var chainId = Hash.Generate();
            var reg = new SmartContractRegistration
            {
                Category = 0,
                ContractBytes = ByteString.CopyFrom(SmartContractZeroCode),
                ContractHash = SmartContractZeroCode.CalculateHash()
            };

            var chain = await _chainCreationService.CreateNewChainAsync(chainId, new List<SmartContractRegistration>{reg});
            _stateDictator.ChainId = chainId;
            return chain;
        }
        
        internal IMiner GetMiner(IMinerConfig config, TxPoolService poolService, ClientManager clientManager = null)
        {
            var miner = new AElf.Miner.Miner.Miner(config, poolService, _chainService, _stateDictator,
                _concurrencyExecutingService, _transactionManager, _transactionResultManager, _logger,
                clientManager);

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
        internal ContractTxPool CreateContractTxPool()
        {
            var poolconfig = TxPoolConfig.Default;
            return new ContractTxPool(poolconfig, _logger);
        }

        internal PriorTxPool CreatePriorTxPool()
        {
            var poolconfig = TxPoolConfig.Default;
            return new PriorTxPool(poolconfig, _logger);
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
            mock.Setup(lc => lc.GetCurrentBlockHeightAsync()).Returns(Task.FromResult((ulong)Headers.Count - 1));
            mock.Setup(lc => lc.GetHeaderByHeightAsync(It.IsAny<ulong>()))
                .Returns<ulong>(p => Task.FromResult(SideChainHeaders[(int) p]));

            return mock;
        }

        private Mock<IBlockChain> MockBlockChain()
        {
            Mock<IBlockChain> mock = new Mock<IBlockChain>();
            mock.Setup(bc => bc.GetBlockByHeightAsync(It.IsAny<ulong>()))
                .Returns<ulong>(p => Task.FromResult(Blocks[(int) p]));
            return mock;
        }

        public Mock<IChainService> MockChainService()
        {
            Mock<IChainService> mock = new Mock<IChainService>();
            mock.Setup(cs => cs.GetLightChain(It.IsAny<Hash>())).Returns(MockLightChain().Object);
            mock.Setup(cs => cs.GetBlockChain(It.IsAny<Hash>())).Returns(MockBlockChain().Object);
            return mock;
        }

        public IBlockHeader MockBlockHeader()
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
                ChainId = chainId ?? Hash.Generate()
            };
        }
        
        public Mock<IBlock> MockBlock(IBlockHeader header, IBlockBody body)
        {
            Mock<IBlock> mock = new Mock<IBlock>();
            mock.Setup(b => b.Header).Returns((BlockHeader)header);
            mock.Setup(b => b.Body).Returns((BlockBody)body);
            return mock;
        }

        public ParentChainBlockInfoRpcServerImpl MockParentChainBlockInfoRpcServerImpl()
        {
            return new ParentChainBlockInfoRpcServerImpl(MockChainService().Object, _logger);
        }

        public SideChainBlockInfoRpcServerImpl MockSideChainBlockInfoRpcServerImpl()
        {
            return new SideChainBlockInfoRpcServerImpl(MockChainService().Object, _logger);
        }
        
        public ServerManager ServerManager(ParentChainBlockInfoRpcServerImpl impl1, SideChainBlockInfoRpcServerImpl impl2)
        {
            return new ServerManager(impl1, impl2);
        }
        
        public ServerManager ServerManager()
        {
            return new ServerManager(MockParentChainBlockInfoRpcServerImpl(), MockSideChainBlockInfoRpcServerImpl());
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
            SideChainHeaders = new List<IBlockHeader>
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

        public Hash MockParentChainServer(int port, string address, string dir)
        {
            
            var chainId = Hash.Generate();
            
            Headers = new List<IBlockHeader>
            {
                MockBlockHeader(),
                MockBlockHeader(),
                MockBlockHeader()
            };
            //IBlockHeader blockHeader = Headers[0];
            Blocks = new List<IBlock>
            {
                MockBlock(Headers[0], MockBlockBody(0, chainId)).Object,
                MockBlock(Headers[1], MockBlockBody(1, chainId)).Object,
                MockBlock(Headers[2], MockBlockBody(2, chainId)).Object
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