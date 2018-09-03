using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AElf.ChainController;
using AElf.ChainController.TxMemPool;
using AElf.Cryptography.ECDSA;
using AElf.SmartContract;
using AElf.Kernel.Managers;
using AElf.Miner.Miner;
using AElf.Miner.Rpc.Client;
using AElf.Miner.Tests;
using Akka.Actor;
using Akka.TestKit;
using Akka.TestKit.Xunit;
using Google.Protobuf;
using Xunit;
using Xunit.Frameworks.Autofac;
using AElf.Runtime.CSharp;
using Moq;
using NLog;
using MinerConfig = AElf.Miner.Miner.MinerConfig;

namespace AElf.Kernel.Tests.Miner
{
    [UseAutofacTestFramework]
    public class MinerLifetime : TestKitBase
    {
        // IncrementId is used to differentiate txn
        // which is identified by From/To/IncrementId
        private static int _incrementId = 0;

        public ulong NewIncrementId()
        {
            var res = _incrementId;
            var n = Interlocked.Increment(ref _incrementId);
            return (ulong) res;
        }

        private ActorSystem sys = ActorSystem.Create("test");
        private IActorRef _generalExecutor;
        private IChainCreationService _chainCreationService;
        private readonly ILogger _logger;
        private IStateDictator _stateDictator;
        private ISmartContractManager _smartContractManager;

        private IActorRef _serviceRouter;
        private ISmartContractRunnerFactory _smartContractRunnerFactory;
        private ISmartContractService _smartContractService;
        private IChainContextService _chainContextService;
        private IAccountContextService _accountContextService;
        private ITransactionManager _transactionManager;
        private ITransactionResultManager _transactionResultManager;
        private IExecutingService _concurrencyExecutingService;
        private IFunctionMetadataService _functionMetadataService;
        private IChainService _chainService;
        private readonly IChainManagerBasic _chainManagerBasic;
        private readonly IBlockManagerBasic _blockManagerBasic;
        
        public MinerLifetime(IStateDictator stateDictator, 
            IChainCreationService chainCreationService, 
            IChainContextService chainContextService, ILogger logger, IAccountContextService accountContextService, 
            ITransactionManager transactionManager, ITransactionResultManager transactionResultManager, 
            IChainService chainService, ISmartContractManager smartContractManager, 
            IFunctionMetadataService functionMetadataService, 
            IExecutingService concurrencyExecutingService, IChainManagerBasic chainManagerBasic, IBlockManagerBasic blockManagerBasic) : base(new XunitAssertions())
        {
            _chainCreationService = chainCreationService;
            _chainContextService = chainContextService;
            _logger = logger;
            _accountContextService = accountContextService;
            _transactionManager = transactionManager;
            _transactionResultManager = transactionResultManager;

            _chainService = chainService;
            _smartContractManager = smartContractManager;
            _functionMetadataService = functionMetadataService;
            _concurrencyExecutingService = concurrencyExecutingService;
            _chainManagerBasic = chainManagerBasic;
            _blockManagerBasic = blockManagerBasic;

            _stateDictator = stateDictator;
            _stateDictator.BlockProducerAccountAddress = Hash.Generate();
            Initialize();
        }

        private void Initialize()
        {
            _smartContractRunnerFactory = new SmartContractRunnerFactory();
            var runner = new SmartContractRunner("../../../../AElf.SDK.CSharp/bin/Debug/netstandard2.0/");
            _smartContractRunnerFactory.AddRunner(0, runner);
            _smartContractService = new SmartContractService(_smartContractManager, _smartContractRunnerFactory, _stateDictator, _functionMetadataService);
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

        public Mock<ITxPoolService> MockTxPoolService(Hash chainId)
        {
            var contractAddressZero = new Hash(chainId.CalculateHashWith(Globals.GenesisBasicContract)).ToAccount();

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
            
            var txs = new List<Transaction>(){
                txnDep
            };
            
            var mock = new Mock<ITxPoolService>();
            mock.Setup((s) => s.GetReadyTxsAsync(3000)).Returns(Task.FromResult(txs));
            return mock;
        }
        
        
        public List<Transaction> CreateTxs(Hash chainId)
        {
            var contractAddressZero = new Hash(chainId.CalculateHashWith(Globals.GenesisBasicContract)).ToAccount();

            var code = ExampleContractCode;

            var regExample = new SmartContractRegistration
            {
                Category = 0,
                ContractBytes = ByteString.CopyFrom(code),
                ContractHash = code.CalculateHash()
            };
            
            
            ECKeyPair keyPair = new KeyPairGenerator().Generate();
            ECSigner signer = new ECSigner();
            
            var txPrint = new Transaction()
            {
                From = keyPair.GetAddress(),
                To = contractAddressZero,
                IncrementId = NewIncrementId(),
                MethodName = "Print",
                Params = ByteString.CopyFrom(new Parameters()
                {
                    Params = {
                        new Param
                        {
                            StrVal = "AElf"
                        }
                    }
                }.ToByteArray()),
                
                Fee = TxPoolConfig.Default.FeeThreshold + 1
            };
            
            Hash hash = txPrint.GetHash();

            ECSignature signature = signer.Sign(keyPair, hash.GetHashBytes());
            txPrint.P = ByteString.CopyFrom(keyPair.PublicKey.Q.GetEncoded());
            txPrint.R = ByteString.CopyFrom(signature.R); 
            txPrint.S = ByteString.CopyFrom(signature.S);
            
            var txs = new List<Transaction>(){
                txPrint
            };

            return txs;
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

            var chain = await _chainCreationService.CreateNewChainAsync(chainId, new List<SmartContractRegistration>{reg});
            _stateDictator.ChainId = chainId;
            return chain;
        }
        
        public IMiner GetMiner(IMinerConfig config, TxPoolService poolService)
        {
            var miner = new AElf.Miner.Miner.Miner(config, poolService, _chainService, _stateDictator,
                _concurrencyExecutingService, _transactionManager, _transactionResultManager, _logger,
                new MinerClientGenerator(_logger));

            return miner;
        }

        public IMinerConfig GetMinerConfig(Hash chainId, ulong txCountLimit, byte[] getAddress)
        {
            return new MinerConfig
            {
                ChainId = chainId,
                CoinBase = getAddress
            };
        }
       
        
        [Fact]
        public async Task Mine()
        {
            var keypair = new KeyPairGenerator().Generate();
            var chain = await CreateChain();
            var minerconfig = GetMinerConfig(chain.Id, 10, keypair.GetAddress());
            var poolconfig = TxPoolConfig.Default;
            poolconfig.ChainId = chain.Id;
            var pool = new ContractTxPool(poolconfig, _logger);
            var dPoSPool = new PriorTxPool(poolconfig, _logger);
            var poolService = new TxPoolService(pool, _accountContextService, _logger, dPoSPool);
            poolService.Start();

            var txs = CreateTxs(chain.Id);
            foreach (var tx in txs)
            {
                await poolService.AddTxAsync(tx);
            }
            
            var miner = GetMiner(minerconfig, poolService);

            miner.Init(keypair);
            
            var block = await miner.Mine(Timeout.Infinite, false);
            
            Assert.NotNull(block);
            Assert.Equal((ulong)1, block.Header.Index);
            
            byte[] uncompressedPrivKey = block.Header.P.ToByteArray();
            Hash addr = uncompressedPrivKey.Take(ECKeyPair.AddressLength).ToArray();
            Assert.Equal(minerconfig.CoinBase, addr);
            
            ECKeyPair recipientKeyPair = ECKeyPair.FromPublicKey(uncompressedPrivKey);
            ECVerifier verifier = new ECVerifier(recipientKeyPair);
            Assert.True(verifier.Verify(block.Header.GetSignature(), block.Header.GetHash().GetHashBytes()));

        }
        
        [Fact]
        public async Task ExecuteWithoutTransaction()
        {
            var keypair = new KeyPairGenerator().Generate();
            var chain = await CreateChain();
            var minerconfig = GetMinerConfig(chain.Id, 10, keypair.GetAddress());
            var poolconfig = TxPoolConfig.Default;
            poolconfig.ChainId = chain.Id;
            var pool = new ContractTxPool(poolconfig, _logger);
            var dPoSPool = new PriorTxPool(poolconfig, _logger);
            var poolService = new TxPoolService(pool, _accountContextService, _logger, dPoSPool);
            
            poolService.Start();

            var miner = GetMiner(minerconfig, poolService);
            
            /*var parallelTransactionExecutingService = new ParallelTransactionExecutingService(_requestor,
                new Grouper(_servicePack.ResourceDetectionService));*/
            
            miner.Init(keypair);
            
            var block = await miner.Mine(Timeout.Infinite, false);
            
            Assert.NotNull(block);
            Assert.Equal((ulong)1, block.Header.Index);
            
            byte[] uncompressedPrivKey = block.Header.P.ToByteArray();
            Hash addr = uncompressedPrivKey.Take(ECKeyPair.AddressLength).ToArray();
            Assert.Equal(minerconfig.CoinBase, addr);
            
            ECKeyPair recipientKeyPair = ECKeyPair.FromPublicKey(uncompressedPrivKey);
            ECVerifier verifier = new ECVerifier(recipientKeyPair);
            Assert.True(verifier.Verify(block.Header.GetSignature(), block.Header.GetHash().GetHashBytes()));

        }
    }
}