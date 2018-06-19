using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AElf.Kernel.Concurrency;
using AElf.Kernel.Concurrency.Execution;
using AElf.Kernel.Concurrency.Execution.Messages;
using AElf.Kernel.Extensions;
using AElf.Kernel.KernelAccount;
using AElf.Kernel.Miner;
using AElf.Kernel.Managers;
using AElf.Kernel.Services;
using AElf.Kernel.SmartContracts.CSharpSmartContract;
using AElf.Kernel.Tests.Concurrency.Execution;
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
            var n = Interlocked.Increment(ref _incrementId);
            return (ulong)n;
        }

        private ActorSystem sys = ActorSystem.Create("test");
        private readonly IChainContextService _chainContextService;
        private readonly IBlockGenerationService _blockGenerationService;
        //private ChainContextWithSmartContractZeroWithTransfer _chainContext;
        //private SmartContractZeroWithTransfer SmartContractZero { get { return (_chainContext.SmartContractZero as SmartContractZeroWithTransfer); } }
        private AccountContextService _accountContextService;
        private IActorRef _generalExecutor;
        private IChainCreationService _chainCreationService;

        private IWorldStateManager _worldStateManager;
        private ISmartContractManager _smartContractManager;

        private MockSetup _mock;
        private IActorRef _serviceRouter;
        private ISmartContractRunnerFactory _smartContractRunnerFactory = new SmartContractRunnerFactory();
        private ISmartContractService _smartContractService;
        private readonly IBlockVaildationService blockVaildationService;

        public MinerLifetime(MockSetup mock, IWorldStateManager worldStateManager,
            AccountContextService accountContextService, ISmartContractManager smartContractManager,
            IBlockGenerationService blockGenerationService, IChainCreationService chainCreationService, IChainContextService chainContextService, IBlockVaildationService blockVaildationService) : base(new XunitAssertions())
        {
            //_chainContextService = chainContextService;
            //_chainContext = chainContext;
            _accountContextService = accountContextService;
            _blockGenerationService = blockGenerationService;
            _chainCreationService = chainCreationService;
            _chainContextService = chainContextService;
            this.blockVaildationService = blockVaildationService;
            _mock = mock;

            _worldStateManager = worldStateManager;
            _smartContractManager = smartContractManager;

            _serviceRouter = sys.ActorOf(LocalServicesProvider.Props(_mock.ServicePack));
            _generalExecutor = sys.ActorOf(GeneralExecutor.Props(sys, _serviceRouter), "exec");
            //_generalExecutor = sys.ActorOf(GeneralExecutor.Props(sys, _chainContextService, _accountContextService), "exec");
            var runner = new SmartContractRunner(ContractCodes.TestContractFolder);
            _smartContractRunnerFactory.AddRunner(0, runner);
            _smartContractService = new SmartContractService(_smartContractManager, _smartContractRunnerFactory, _worldStateManager);
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

        public Mock<ITxPoolService> MockTxPoolService()
        {
            
            var balances = new List<int>()
            {
                100, 0
            };
            var addresses = Enumerable.Range(0, balances.Count).Select(x => Hash.Generate()).ToList();

            foreach (var addbal in addresses.Zip(balances, Tuple.Create))
            {
                _mock.Initialize1(addbal.Item1, (ulong)addbal.Item2);
            }

            var txs = new List<ITransaction>(){
                GetTransaction(addresses[0], addresses[1], 10),
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
            //var genesis = await _blockManager.GetBlockAsync(chain.GenesisBlockHash);

            var code = ExampleContractCode;

            var regExample = new SmartContractRegistration
            {
                Category = 0,
                ContractBytes = ByteString.CopyFrom(code),
                ContractHash = code.CalculateHash()
            };

            var contractAddressZero = chainId.CalculateHashWith("__SmartContractZero__");

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

            var txnCtxt = new TransactionContext()
            {
                Transaction = txnDep
            };

            var executive = await _smartContractService.GetExecutiveAsync(contractAddressZero, chainId);
            await executive.SetTransactionContext(txnCtxt).Apply();

            var address = txnCtxt.Trace.RetVal.DeserializeToPbMessage<Hash>();

            //var chain = await _chainCreationService.CreateNewChainAsync(chainId, reg);
            //var chainContext = _chainContextService.GetChainContext(chainId);
            
            //var reg = new SmartContractRegistration
            //{
            //    Category = 1,
            //    ContractBytes = ByteString.CopyFromUtf8(smartContract.AssemblyQualifiedName),
            //    ContractHash = Hash.Zero
            //};

            //var deplotment = new SmartContractDeployment
            //{
            //    ContractHash = Hash.Zero
            //};
            //await chainContext.SmartContractZero.RegisterSmartContract(reg);
            //await chainContext.SmartContractZero.DeploySmartContract(deplotment);

            return chain;
        }
        
        public IMiner GetMiner(IMinerConfig config)
        {
            var parallelTransactionExecutingService = new ParallelTransactionExecutingService(sys);
            return new Kernel.Miner.Miner(_blockGenerationService, config, MockTxPoolService().Object, 
                parallelTransactionExecutingService);
        }

        public IMinerConfig GetMinerConfig(Hash chainId, ulong txCountLimit)
        {
            return new MinerConfig
            {
                TxCount = txCountLimit
            };
        }
        
        private Transaction GetTransaction(Hash from, Hash to, ulong qty)
        {
            // TODO: Test with IncrementId
            TransferArgs args = new TransferArgs()
            {
                From = from,
                To = to,
                Quantity = qty
            };

            ByteString argsBS = args.ToByteString();

            Transaction tx = new Transaction()
            {
                IncrementId = 0,
                From = from,
                To = to,
                MethodName = "Transfer",
                Params = argsBS
            };

            return tx;
        }
        

        [Fact]
        public async Task MineWithoutStarting()
        {
            var config = GetMinerConfig(_mock.ChainId1, 10);
            var miner = GetMiner(config);
            
            var block = await miner.Mine();
            Assert.Null(block);
        }

        [Fact(Skip = "TODO")]
        public async Task Mine()
        {
            var config = GetMinerConfig(_mock.ChainId1, 10);
            var miner = GetMiner(config);

            //_chainContextService.AddChainContext(_mock.ChainId1, _chainContext);
            _generalExecutor.Tell(new RequestAddChainExecutor(_mock.ChainId1));
            ExpectMsg<RespondAddChainExecutor>();
            miner.Start();
            var block = await miner.Mine();
            Assert.NotNull(block);
        }
        
    }
}