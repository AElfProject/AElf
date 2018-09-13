using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using AElf.ChainController;
using AElf.ChainController.TxMemPool;
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
using Moq;
using NLog;

namespace AElf.Miner.Tests.Grpc
{
    public class MockSetup
    {
        public List<IBlockHeader> _headers = new List<IBlockHeader>();
        public readonly ILogger _logger;
        public ulong _i;
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
                clientManager, MinerServer());

            return miner;
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
        
        public Mock<ILightChain> MockLightChain()
        {
            Mock<ILightChain> mock = new Mock<ILightChain>();
            mock.Setup(lc => lc.GetCurrentBlockHeightAsync()).Returns(Task.FromResult((ulong)_headers.Count - 1));
            mock.Setup(lc => lc.GetHeaderByHeightAsync(It.IsAny<ulong>()))
                .Returns<ulong>(p => Task.FromResult(_headers[(int) p]));

            return mock;
        }

        public Mock<IChainService> MockChainService()
        {
            Mock<IChainService> mock = new Mock<IChainService>();
            mock.Setup(cs => cs.GetLightChain(It.IsAny<Hash>())).Returns(MockLightChain().Object);
            return mock;
        }

        public Mock<IBlockHeader> MockBlockHeader()
        {
            Mock<IBlockHeader> mock = new Mock<IBlockHeader>();
            mock.Setup(bh => bh.GetHash()).Returns(Hash.Generate());
            mock.Setup(bh => bh.MerkleTreeRootOfTransactions).Returns(Hash.Generate());
            return mock;
        }
        
        
        public SideChainServer MinerServer()
        {
            GrpcLocalConfig.Instance.SideChainServer = false;
            return new SideChainServer(_logger, new SideChainHeaderInfoRpcServerImpl(MockChainService().Object, _logger));
        }

        public Mock<IChainManagerBasic> MockChainManager()
        {
            var mock = new Mock<IChainManagerBasic>();
            mock.Setup(cm => cm.GetCurrentBlockHeightsync(It.IsAny<Hash>())).Returns(() =>
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
        
        public Hash MockServer(int port, string address, string dir)
        {
            _headers = new List<IBlockHeader>
            {
                MockBlockHeader().Object,
                MockBlockHeader().Object,
                MockBlockHeader().Object
            };

            var server = MinerServer();
            var sideChainId = Hash.Generate();
            MockKeyPair(sideChainId, dir);
            //start server, sidechain is server-side
            GrpcLocalConfig.Instance.LocalSideChainServerPort = port;
            GrpcLocalConfig.Instance.LocalServerIP = address;
            server.Init(sideChainId, dir);
            server.StartUp();
            
            GrpcRemoteConfig.Instance.ChildChains = new Dictionary<string, Uri>
            {
                {
                    sideChainId.ToHex(), new Uri{
                        Address = GrpcLocalConfig.Instance.LocalServerIP,
                        Port = GrpcLocalConfig.Instance.LocalSideChainServerPort
                    }
                }
            };
            return sideChainId;
        }

        public void CreateDirectory(string dir)
        {
            if(Directory.Exists(Path.Combine(dir, "certs")))
                Directory.Delete(Path.Combine(dir, "certs"), true);
        }
    }
}