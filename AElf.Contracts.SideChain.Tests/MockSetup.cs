//using System.Collections.Generic;
//using System.IO;
//using System.Threading;
//using System.Threading.Tasks;
//using AElf.ChainController;
//using AElf.Kernel;
//using AElf.SmartContract;
//using Google.Protobuf;
//using AElf.Common;
//using AElf.Kernel.SmartContract.Domain;
//using AElf.Kernel.SmartContractExecution.Domain;
//using AElf.Runtime.CSharp;
//using AElf.SmartContract.Contexts;
//using Microsoft.Extensions.Logging;
//using Microsoft.Extensions.Logging.Abstractions;
//using Volo.Abp.DependencyInjection;
//
//namespace AElf.Contracts.SideChain.Tests
//{
//    public class MockSetup : ITransientDependency
//    {
//        // IncrementId is used to differentiate txn
//        // which is identified by From/To/IncrementId
//        private static int _incrementId = 0;
//        public ulong NewIncrementId()
//        {
//            var n = Interlocked.Increment(ref _incrementId);
//            return (ulong)n;
//        }
//
//        public int ChainId1 { get; } = ChainHelpers.GetRandomChainId();
//        public IStateManager StateManager => StateProviderFactory.CreateStateManager();
//        public IStateProviderFactory StateProviderFactory { get; private set; }
//        public ISmartContractManager SmartContractManager;
//        public ISmartContractService SmartContractService;
//        public IChainService ChainService;
//
//        private IChainCreationService _chainCreationService;
//
//        public ILogger<MockSetup> Logger {get;set;}
//
//        public MockSetup(ISmartContractManager smartContractManager,
//            IStateProviderFactory stateProviderFactory,
//            IChainCreationService chainCreationService, IChainService chainService,
//            ISmartContractService smartContractService)
//        {
//            Logger = NullLogger<MockSetup>.Instance;
//            SmartContractManager = smartContractManager;
//            StateProviderFactory = stateProviderFactory;
//            _chainCreationService = chainCreationService;
//            ChainService = chainService;
//            SmartContractService = smartContractService;
//            Initialize();
//        }
//
//        private void Initialize()
//        {
//            Task.Factory.StartNew(async () =>
//            {
//                await Init();
//            }).Unwrap().Wait();
//        }
//
//        public byte[] CrossChainCode
//        {
//            get
//            {
//                var filePath = Path.GetFullPath("../../../../AElf.Contracts.CrossChain/bin/Debug/netstandard2.0/AElf.Contracts.CrossChain.dll");
//                return File.ReadAllBytes(filePath);
//            }
//        }
//
//        public byte[] SCZeroContractCode
//        {
//            get
//            {
//                var filePath = Path.GetFullPath("../../../../AElf.Contracts.Genesis/bin/Debug/netstandard2.0/AElf.Contracts.Genesis.dll");
//                return File.ReadAllBytes(filePath);
//            }
//        }
//
//        private async Task Init()
//        {
//            var reg1 = new SmartContractRegistration
//            {
//                Category = 0,
//                ContractBytes = ByteString.CopyFrom(CrossChainCode),
//                ContractHash = Hash.FromRawBytes(CrossChainCode),
//                SerialNumber = GlobalConfig.CrossChainContract
//            };
//            var reg0 = new SmartContractRegistration
//            {
//                Category = 0,
//                ContractBytes = ByteString.CopyFrom(SCZeroContractCode),
//                ContractHash = Hash.FromRawBytes(SCZeroContractCode),
//                SerialNumber = GlobalConfig.GenesisBasicContract
//            };
//
//            var chain1 =
//                await _chainCreationService.CreateNewChainAsync(ChainId1,
//                    new List<SmartContractRegistration> {reg0, reg1});
//        }
//
//        public async Task<IExecutive> GetExecutiveAsync(Address address)
//        {
//            var executive = await SmartContractService.GetExecutiveAsync(address, ChainId1);
//            return executive;
//        }
//    }
//}