using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using AElf.ChainController;
using AElf.Kernel;
using AElf.SmartContract;
using Google.Protobuf;
using AElf.Common;
using AElf.Kernel.Managers;
using AElf.Runtime.CSharp;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Volo.Abp.DependencyInjection;

namespace AElf.Contracts.SideChain.Tests
{
    public class MockSetup : ITransientDependency
    {
        // IncrementId is used to differentiate txn
        // which is identified by From/To/IncrementId
        private static int _incrementId = 0;
        public ulong NewIncrementId()
        {
            var n = Interlocked.Increment(ref _incrementId);
            return (ulong)n;
        }

        public Hash ChainId1 { get; } = Hash.LoadByteArray(ChainHelpers.GetRandomChainId());
        public IStateManager StateManager { get; private set; }
        public ISmartContractManager SmartContractManager;
        public ISmartContractService SmartContractService;
        public IChainService ChainService;
        private IFunctionMetadataService _functionMetadataService;

        private IChainCreationService _chainCreationService;

        public ILogger<MockSetup> Logger {get;set;}
        private ISmartContractRunnerContainer _smartContractRunnerContainer;


        private IBlockManager _blockManager;
        private IChainManager _chainManager;
        private ITransactionManager _transactionManager;
        private ITransactionTraceManager _transactionTraceManager;


        public MockSetup(ITransactionManager transactionManager, IBlockManager blockManager
            , IChainManager chainManager, ISmartContractManager smartContractManager,
            ITransactionTraceManager transactionTraceManager,IFunctionMetadataService functionMetadataService,
            IStateManager stateManager)
        {
            Logger = NullLogger<MockSetup>.Instance;
            _transactionManager = transactionManager;
            _chainManager = chainManager;
            _blockManager = blockManager;
            SmartContractManager = smartContractManager;
            _transactionTraceManager = transactionTraceManager;
            _functionMetadataService = functionMetadataService;
            StateManager = stateManager;
            Initialize();
        }

        private void Initialize()
        {
            ChainService = new ChainService(_chainManager, _blockManager,
                _transactionManager, _transactionTraceManager, StateManager);
            _smartContractRunnerContainer = new SmartContractRunnerContainer();
            var runner =
                new SmartContractRunner("../../../../AElf.Runtime.CSharp.Tests.TestContract/bin/Debug/netstandard2.0/");
            _smartContractRunnerContainer.AddRunner(0, runner);
            _chainCreationService = new ChainCreationService(ChainService,
                new SmartContractService(SmartContractManager, _smartContractRunnerContainer,
                    StateManager, _functionMetadataService, ChainService));
            Task.Factory.StartNew(async () =>
            {
                await Init();
            }).Unwrap().Wait();
            SmartContractService = new SmartContractService(SmartContractManager, _smartContractRunnerContainer, StateManager, _functionMetadataService, ChainService);
        }

        public byte[] CrossChainCode
        {
            get
            {
                var filePath = Path.GetFullPath("../../../../AElf.Contracts.CrossChain/bin/Debug/netstandard2.0/AElf.Contracts.CrossChain.dll");
                return File.ReadAllBytes(filePath);
            }
        }

        public byte[] SCZeroContractCode
        {
            get
            {
                var filePath = Path.GetFullPath("../../../../AElf.Contracts.Genesis/bin/Debug/netstandard2.0/AElf.Contracts.Genesis.dll");
                return File.ReadAllBytes(filePath);
            }
        }

        private async Task Init()
        {
            var reg1 = new SmartContractRegistration
            {
                Category = 0,
                ContractBytes = ByteString.CopyFrom(CrossChainCode),
                ContractHash = Hash.FromRawBytes(CrossChainCode),
                SerialNumber = GlobalConfig.CrossChainContract
            };
            var reg0 = new SmartContractRegistration
            {
                Category = 0,
                ContractBytes = ByteString.CopyFrom(SCZeroContractCode),
                ContractHash = Hash.FromRawBytes(SCZeroContractCode),
                SerialNumber = GlobalConfig.GenesisBasicContract
            };

            var chain1 =
                await _chainCreationService.CreateNewChainAsync(ChainId1,
                    new List<SmartContractRegistration> {reg0, reg1});
        }

        public async Task<IExecutive> GetExecutiveAsync(Address address)
        {
            var executive = await SmartContractService.GetExecutiveAsync(address, ChainId1);
            return executive;
        }
    }
}