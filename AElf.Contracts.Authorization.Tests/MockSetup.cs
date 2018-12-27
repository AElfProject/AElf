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
using NLog;

namespace AElf.Contracts.Authorization.Tests
{
    public class MockSetup
    {
        // IncrementId is used to differentiate txn
        // which is identified by From/To/IncrementId
        private static int _incrementId = 0;

        public ulong NewIncrementId()
        {
            var n = Interlocked.Increment(ref _incrementId);
            return (ulong) n;
        }

        public Hash ChainId { get; } = Hash.LoadByteArray(ChainHelpers.GetRandomChainId());

        public IStateManager StateManager { get; private set; }
        public ISmartContractService SmartContractService;
        public IChainService ChainService;

        private IFunctionMetadataService _functionMetadataService;
        private IChainCreationService _chainCreationService;
        private ISmartContractRunnerContainer _smartContractRunnerContainer;
        private ILogger _logger;
        private IChainManager _chainManager;
        private ITransactionManager _transactionManager;
        private IBlockManager _blockManager;
        private ISmartContractManager _smartContractManager;
        private ITransactionTraceManager _transactionTraceManager;

        public MockSetup(ILogger logger, IBlockManager blockManager, ITransactionManager transactionManager
            , IChainManager chainManager, ISmartContractManager smartContractManager,
            ITransactionTraceManager transactionTraceManager,IFunctionMetadataService functionMetadataService,
            IStateManager stateManager)
        {
            _logger = logger;
            _blockManager = blockManager;
            _chainManager = chainManager;
            _transactionManager = transactionManager;
            _smartContractManager = smartContractManager;
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
                new SmartContractService(_smartContractManager, _smartContractRunnerContainer,
                    StateManager, _functionMetadataService), _logger);
            Task.Factory.StartNew(async () => { await Init(); }).Unwrap().Wait();
            SmartContractService = new SmartContractService(_smartContractManager, _smartContractRunnerContainer,
                StateManager, _functionMetadataService);
            ChainService = new ChainService(_chainManager, _blockManager, _transactionManager,
                _transactionTraceManager, StateManager);
        }

        public byte[] AuthorizationCode
        {
            get
            {
                var filePath = Path.GetFullPath("../../../../AElf.Contracts.Authorization/bin/Debug/netstandard2.0/AElf.Contracts.Authorization.dll");
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
                ContractBytes = ByteString.CopyFrom(AuthorizationCode),
                ContractHash = Hash.FromRawBytes(AuthorizationCode),
                SerialNumber = GlobalConfig.AuthorizationContract
            };
            var reg0 = new SmartContractRegistration
            {
                Category = 0,
                ContractBytes = ByteString.CopyFrom(SCZeroContractCode),
                ContractHash = Hash.FromRawBytes(SCZeroContractCode),
                SerialNumber = GlobalConfig.GenesisBasicContract
            };

            var chain1 =
                await _chainCreationService.CreateNewChainAsync(ChainId,
                    new List<SmartContractRegistration> {reg0, reg1});
        }

        public async Task<IExecutive> GetExecutiveAsync(Address address)
        {
            var executive = await SmartContractService.GetExecutiveAsync(address, ChainId);
            return executive;
        }
    }
}