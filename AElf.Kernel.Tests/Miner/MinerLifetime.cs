using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AElf.Cryptography.ECDSA;
using AElf.Kernel.Concurrency;
using AElf.Kernel.Concurrency.Execution;
using AElf.Kernel.Concurrency.Execution.Messages;
using AElf.Kernel.Extensions;
using AElf.Kernel.KernelAccount;
using AElf.Kernel.Miner;
using AElf.Kernel.Managers;
using AElf.Kernel.Services;
using AElf.Kernel.SmartContracts.CSharpSmartContract;
using AElf.Kernel.Storages;
using AElf.Kernel.Tests.Concurrency.Execution;
using AElf.Kernel.Tests.Concurrency.Scheduling;
using AElf.Kernel.TxMemPool;
using Akka.Actor;
using Akka.Dispatch.SysMsg;
using Akka.TestKit;
using Akka.TestKit.Xunit;
using Google.Protobuf;
using Moq;
using Xunit;
using Xunit.Frameworks.Autofac;
using ServiceStack;
using AElf.Runtime.CSharp;
using NLog;
using AElf.Types.CSharp;

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
        private IWorldStateManager _worldStateManager;
        private ISmartContractManager _smartContractManager;

        private IActorRef _serviceRouter;
        private ISmartContractRunnerFactory _smartContractRunnerFactory = new SmartContractRunnerFactory();
        private ISmartContractService _smartContractService;
        private IChainContextService _chainContextService;
        private IAccountContextService _accountContextService;
        private ITransactionManager _transactionManager;
        private ITransactionResultManager _transactionResultManager;
        private IChainManager _chainManager;
        private readonly IBlockManager _blockManager;

        
        public MinerLifetime(IWorldStateManager worldStateManager, ISmartContractStore smartContractStore,
            IChainCreationService chainCreationService, 
            IChainContextService chainContextService, ILogger logger, IAccountContextService accountContextService, 
            ITransactionManager transactionManager, ITransactionResultManager transactionResultManager, 
            IChainManager chainManager, IBlockManager blockManager) : base(new XunitAssertions())
        {
            _chainCreationService = chainCreationService;
            _chainContextService = chainContextService;
            _logger = logger;
            _accountContextService = accountContextService;
            _transactionManager = transactionManager;
            _transactionResultManager = transactionResultManager;
            _chainManager = chainManager;
            _blockManager = blockManager;

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

        public Mock<ITxPoolService> MockTxPoolService(Hash chainId)
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
            
            var mock = new Mock<ITxPoolService>();
            mock.Setup((s) => s.GetReadyTxsAsync(It.IsAny<ulong>())).Returns(Task.FromResult(txs));
            return mock;
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
            /*var txnDep = new Transaction()
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
            };*/
            
            
            
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
            
            var txs = new List<ITransaction>(){
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

            var chain = await _chainCreationService.CreateNewChainAsync(chainId, reg);
            return chain;
        }
        
        public IMiner GetMiner(IMinerConfig config, TxPoolService poolService)
        {
            var parallelTransactionExecutingService = new ParallelTransactionExecutingService(sys);
            return new Kernel.Miner.Miner(config, poolService,
                parallelTransactionExecutingService,  _chainManager, _blockManager, _worldStateManager);
        }

        public IMinerConfig GetMinerConfig(Hash chainId, ulong txCountLimit, byte[] getAddress)
        {
            return new MinerConfig
            {
                TxCount = txCountLimit,
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
            var pool = new TxPool(poolconfig, _logger);
            
            var poolService = new TxPoolService(pool, _accountContextService, _transactionManager,
                _transactionResultManager);
            
            poolService.Start();

            var txs = CreateTxs(chain.Id);
            foreach (var tx in txs)
            {
                await poolService.AddTxAsync(tx);
            }
            
            var miner = GetMiner(minerconfig, poolService);
            
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
            
            miner.Start(keypair);
            
            var block = await miner.Mine();
            
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
            var pool = new TxPool(poolconfig, _logger);
            
            var poolService = new TxPoolService(pool, _accountContextService, _transactionManager,
                _transactionResultManager);
            
            poolService.Start();

            var miner = GetMiner(minerconfig, poolService);
            
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
            
            
            miner.Start(keypair);
            
            var block = await miner.Mine();
            
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