using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using AElf.ChainController;
using AElf.ChainController.CrossChain;
using AElf.Execution;
using AElf.Kernel;
using AElf.SmartContract;
using Google.Protobuf;
using ServiceStack;
using AElf.Common;
using AElf.Common.Serializers;
using AElf.Database;
using AElf.Execution.Execution;
using AElf.Kernel.Manager.Interfaces;
using AElf.Kernel.Manager.Managers;
using AElf.Kernel.Storage.Interfaces;
using AElf.Kernel.Storage.Storages;
using AElf.Miner.TxMemPool;
using AElf.Runtime.CSharp;
using AElf.SmartContract.Metadata;
using NLog;

namespace AElf.Contracts.SideChain.Tests
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

        public Hash ChainId1 { get; } = Hash.LoadByteArray(ChainHelpers.GetRandomChainId());
        public IStateManager StateManager { get; private set; }
        public ISmartContractManager SmartContractManager;
        public ISmartContractService SmartContractService;
        public IChainService ChainService;
        private IFunctionMetadataService _functionMetadataService;

        private IChainCreationService _chainCreationService;

        private ISmartContractRunnerFactory _smartContractRunnerFactory;
        private ILogger _logger;

        private IBlockManager _blockManager;
        private IChainManager _chainManager;
        private ITransactionManager _transactionManager;
        private ITransactionTraceManager _transactionTraceManager;


        public MockSetup(ILogger logger, ITransactionManager transactionManager, IBlockManager blockManager
            , IChainManager chainManager, ISmartContractManager smartContractManager,
            ITransactionTraceManager transactionTraceManager,IFunctionMetadataService functionMetadataService)
        {
            _logger = logger;
            _transactionManager = transactionManager;
            _chainManager = chainManager;
            _blockManager = blockManager;
            SmartContractManager = smartContractManager;
            _transactionTraceManager = transactionTraceManager;
            _functionMetadataService = functionMetadataService;
            Initialize();
        }

        private void Initialize()
        {
            NewStorage();
            ChainService = new ChainService(_chainManager, _blockManager,
                _transactionManager, _transactionTraceManager, StateManager);
            _smartContractRunnerFactory = new SmartContractRunnerFactory();
            var runner =
                new SmartContractRunner("../../../../AElf.Runtime.CSharp.Tests.TestContract/bin/Debug/netstandard2.0/");
            _smartContractRunnerFactory.AddRunner(0, runner);
            _chainCreationService = new ChainCreationService(ChainService,
                new SmartContractService(SmartContractManager, _smartContractRunnerFactory,
                    StateManager, _functionMetadataService), _logger);
            Task.Factory.StartNew(async () => { await Init(); }).Unwrap().Wait();
            SmartContractService = new SmartContractService(SmartContractManager, _smartContractRunnerFactory,
                StateManager, _functionMetadataService);
            ChainService = new ChainService(_chainManager, _blockManager, _transactionManager, _transactionTraceManager,
                StateManager);
        }

        private void NewStorage()
        {
            var db = new InMemoryDatabase();
            StateManager = new StateManager(new StateStore(db, new ProtobufSerializer()));
        }

        public byte[] CrossChainCode
        {
            get
            {
                byte[] code = null;
                using (FileStream file = File.OpenRead(Path.GetFullPath(
                    "../../../../AElf.Contracts.CrossChain/bin/Debug/netstandard2.0/AElf.Contracts.CrossChain.dll")))
                {
                    code = file.ReadFully();
                }

                return code;
            }
        }

        public byte[] SCZeroContractCode
        {
            get
            {
                byte[] code = null;
                using (FileStream file = File.OpenRead(Path.GetFullPath(
                    "../../../../AElf.Contracts.Genesis/bin/Debug/netstandard2.0/AElf.Contracts.Genesis.dll")))
                {
                    code = file.ReadFully();
                }

                return code;
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