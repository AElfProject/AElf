using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Kernel.Concurrency;
using AElf.Kernel.Concurrency.Execution;
using AElf.Kernel.Concurrency.Execution.Messages;
using AElf.Kernel.KernelAccount;
using AElf.Kernel.Miner;
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

namespace AElf.Kernel.Tests.Miner
{
    [UseAutofacTestFramework]
    public class MinerLifetime : TestKitBase
    {
        private ActorSystem sys = ActorSystem.Create("test");
        private readonly ChainContextServiceWithAdd _chainContextService;
        private readonly IBlockGenerationService _blockGenerationService;
        private ChainContextWithSmartContractZeroWithTransfer _chainContext;
        private SmartContractZeroWithTransfer SmartContractZero { get { return (_chainContext.SmartContractZero as SmartContractZeroWithTransfer); } }
        private AccountContextService _accountContextService;
        private IActorRef _generalExecutor;
        private IChainCreationService _chainCreationService;

        public MinerLifetime(ChainContextServiceWithAdd chainContextService, 
            ChainContextWithSmartContractZeroWithTransfer chainContext, AccountContextService accountContextService, 
            IBlockGenerationService blockGenerationService, IChainCreationService chainCreationService) : base(new XunitAssertions())
        {
            _chainContextService = chainContextService;
            _chainContext = chainContext;
            _accountContextService = accountContextService;
            _blockGenerationService = blockGenerationService;
            _chainCreationService = chainCreationService;
            _generalExecutor = sys.ActorOf(GeneralExecutor.Props(sys, _chainContextService, _accountContextService), "exec");
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
                SmartContractZero.SetBalance(addbal.Item1, (ulong)addbal.Item2);
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
            var smartContract = typeof(Class1);
            var chainId = _chainContext.ChainId;
            var chain = await _chainCreationService.CreateNewChainAsync(chainId, smartContract);
            /*var chainContext = _chainContextService.GetChainContext(chainId);
            
            var reg = new SmartContractRegistration
            {
                Category = 1,
                ContractBytes = ByteString.CopyFromUtf8(smartContract.AssemblyQualifiedName),
                ContractHash = Hash.Zero
            };

            var deplotment = new SmartContractDeployment
            {
                ContractHash = Hash.Zero
            };
            await chainContext.SmartContractZero.RegisterSmartContract(reg);
            await chainContext.SmartContractZero.DeploySmartContract(deplotment);#*/

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
            return new MinerConifg
            {
                ChainId = chainId,
                TxCountLimit = txCountLimit,
                IsParallel = true
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
            var config = GetMinerConfig(_chainContext.ChainId, 10);
            var miner = GetMiner(config);
            
            var block = await miner.Mine();
            Assert.Null(block);
        }

        [Fact(Skip = "TODO")]
        public async Task Mine()
        {
            var config = GetMinerConfig(_chainContext.ChainId, 10);
            var miner = GetMiner(config);
            
            _chainContextService.AddChainContext(_chainContext.ChainId, _chainContext);
            _generalExecutor.Tell(new RequestAddChainExecutor(_chainContext.ChainId));
            ExpectMsg<RespondAddChainExecutor>();
            miner.Start();
            var block = await miner.Mine();
            Assert.NotNull(block);
        }
        
    }
}