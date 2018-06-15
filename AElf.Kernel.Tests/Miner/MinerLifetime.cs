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
            var n = Interlocked.Increment(ref _incrementId);
            return (ulong)n;
        }

        private ActorSystem sys = ActorSystem.Create("test");
        private readonly IBlockGenerationService _blockGenerationService;
        private IActorRef _generalExecutor;
        private IChainCreationService _chainCreationService;

        private IWorldStateManager _worldStateManager;
        private ISmartContractManager _smartContractManager;

        private IActorRef _serviceRouter;
        private ISmartContractRunnerFactory _smartContractRunnerFactory = new SmartContractRunnerFactory();
        private ISmartContractService _smartContractService;
        private IChainContextService _chainContextService;

        public MinerLifetime(IWorldStateManager worldStateManager, ISmartContractStore smartContractStore,
            IBlockGenerationService blockGenerationService, IChainCreationService chainCreationService, IChainContextService chainContextService) : base(new XunitAssertions())
        {
            _blockGenerationService = blockGenerationService;
            _chainCreationService = chainCreationService;
            _chainContextService = chainContextService;

            _worldStateManager = worldStateManager;
            _smartContractManager = new SmartContractManager(smartContractStore);

           
        }

        public byte[] SmartContractZeroCode
        {
            get
            {
                byte[] code = null;
                using (FileStream file = File.OpenRead(System.IO.Path.GetFullPath("../../../../AElf.Contracts.SmartContractZero/bin/Debug/netstandard2.0/AElf.Contracts.SmartContractZero.dll")))
                {
                    code = file.ReadFully();
                }
                return code;
            }
        }

        public byte[] ExampleContractCode
        {
            get
            {
                byte[] code = null;
                using (FileStream file = File.OpenRead(System.IO.Path.GetFullPath("../../../../AElf.Contracts.Examples/bin/Debug/netstandard2.0/AElf.Contracts.Examples.dll")))
                {
                    code = file.ReadFully();
                }
                return code;
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
            
            var txnDep = new Transaction()
            {
                From = Hash.Zero,
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
                }.ToByteArray())
            };
            
            var txs = new List<ITransaction>(){
                txnDep
            };
            
            var mock = new Mock<ITxPoolService>();
            mock.Setup((s) => s.GetReadyTxsAsync(It.IsAny<ulong>())).Returns(Task.FromResult(txs));
            return mock;
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
        
        public IMiner GetMiner(IMinerConfig config)
        {
            var parallelTransactionExecutingService = new ParallelTransactionExecutingService(sys);
            return new Kernel.Miner.Miner(_blockGenerationService, config, MockTxPoolService(config.ChainId).Object, 
                parallelTransactionExecutingService);
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
        
       
        public async Task MineWithoutStarting()
        {
            var keypair = new KeyPairGenerator().Generate();

            Hash chainId = Hash.Generate();
            var config = GetMinerConfig(chainId, 10, keypair.GetAddress());
            var miner = GetMiner(config);
            
            var block = await miner.Mine();
            Assert.Null(block);
        }

        [Fact]
        public async Task Mine()
        {
            var keypair = new KeyPairGenerator().Generate();
            var chain = await CreateChain();
            
            var config = GetMinerConfig(chain.Id, 10, keypair.GetAddress());
            var miner = GetMiner(config);
            
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
            
            //_chainContextService.AddChainContext(_mock.ChainId1, _chainContext);
            _generalExecutor.Tell(new RequestAddChainExecutor(chain.Id));
            ExpectMsg<RespondAddChainExecutor>();
            
            
            miner.Start(keypair);
            
            var block = await miner.Mine();
            
            
            Assert.NotNull(block);
            Assert.Equal((ulong)1, block.Header.Index);
            
            byte[] uncompressedPrivKey = block.Header.P.ToByteArray();
            Hash addr = uncompressedPrivKey.Take(ECKeyPair.AddressLength).ToArray();
            Assert.Equal(config.CoinBase, addr);
            
            ECKeyPair recipientKeyPair = ECKeyPair.FromPublicKey(uncompressedPrivKey);
            ECVerifier verifier = new ECVerifier(recipientKeyPair);
            Assert.True(verifier.Verify(block.Header.GetSignature(), block.Header.GetHash().GetHashBytes()));

        }
        
    }
}