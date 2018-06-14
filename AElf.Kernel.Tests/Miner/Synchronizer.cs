using System.IO;
using System.Threading;
using System.Threading.Tasks;
using AElf.Kernel.Concurrency;
using AElf.Kernel.Concurrency.Execution;
using AElf.Kernel.Concurrency.Execution.Messages;
using AElf.Kernel.Extensions;
using AElf.Kernel.Managers;
using AElf.Kernel.Services;
using AElf.Kernel.Tests.Concurrency.Execution;
using AElf.Kernel.TxMemPool;
using Akka.Actor;
using Google.Protobuf;
using ServiceStack;
using Xunit;
using Xunit.Frameworks.Autofac;

using Akka.TestKit;
using Akka.TestKit.Xunit;

namespace AElf.Kernel.Tests.Miner
{
    [UseAutofacTestFramework]
    public class Synchronizer
    {
        private IChainCreationService _chainCreationService;
        private IBlockManager _blockManager;
        private ISmartContractService _smartContractService;
        
        private readonly ITxPoolService _txPoolService;
        private ActorSystem sys = ActorSystem.Create("test");
        
        private readonly IChainManager _chainManager;
        private IActorRef _serviceRouter;
        private IActorRef _generalExecutor;
        private MockSetup _mock;

        public Synchronizer(IChainCreationService chainCreationService, IBlockManager blockManager, 
            ISmartContractService smartContractService, ITxPoolService txPoolService, IChainManager chainManager, MockSetup mock)
        {
            _chainCreationService = chainCreationService;
            _blockManager = blockManager;
            _smartContractService = smartContractService;
            _txPoolService = txPoolService;
            _chainManager = chainManager;
            _mock = mock;

            _serviceRouter = sys.ActorOf(LocalServicesProvider.Props(_mock.ServicePack));
            _generalExecutor = sys.ActorOf(GeneralExecutor.Props(sys, _serviceRouter), "exec");
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
        private Hash ChainId { get; } = Hash.Generate().ToAccount();

        private static int _incrementId = 0;
        
        public ulong NewIncrementId()
        {
            var n = Interlocked.Increment(ref _incrementId);
            return (ulong)n;
        }
        
        public Block GenesisBlock()
        {
            var block = new GenesisBlockBuilder().Build(ChainId).Block;
            return block;
        }
        
        public async Task<Chain> CreateChain()
        {
            var reg = new SmartContractRegistration
            {
                Category = 0,
                ContractBytes = ByteString.CopyFrom(SmartContractZeroCode),
                ContractHash = Hash.Zero
            };

            var chain = await _chainCreationService.CreateNewChainAsync(ChainId, reg);
            var genesis = await _blockManager.GetBlockAsync(chain.GenesisBlockHash);

            var code = ExampleContractCode;

            var regExample = new SmartContractRegistration
            {
                Category = 0,
                ContractBytes = ByteString.CopyFrom(code),
                ContractHash = code.CalculateHash()
            };

            var contractAddressZero = ChainId.CalculateHashWith("__SmartContractZero__");

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

            var executive = await _smartContractService.GetExecutiveAsync(contractAddressZero, ChainId);
            await executive.SetTransactionContext(txnCtxt).Apply();

            return (Chain) chain;
        }
        
        public void SyncValidBlock()
        {
            
        }

        [Fact(Skip = "TODO")]
        public async Task SyncGenesisBlock()
        {
            IParallelTransactionExecutingService parallelTransactionExecutingService =
            new ParallelTransactionExecutingService(sys);
            var genesisBlock = GenesisBlock();
            var synchronizer = new Kernel.Miner.Synchronizer(_txPoolService, parallelTransactionExecutingService, _chainManager, _blockManager);
            var res = await synchronizer.ExecuteBlock(genesisBlock);
            Assert.True(res);
        }
    }
}