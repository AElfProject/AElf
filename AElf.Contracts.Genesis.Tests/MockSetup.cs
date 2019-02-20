//using System.Collections.Generic;
//using System.IO;
//using System.Threading;
//using System.Threading.Tasks;
//using AElf.Kernel;
//using AElf.Kernel.ChainController;
//using AElf.SmartContract;
//using Google.Protobuf;
//using AElf.Common;
//using AElf.Kernel.SmartContractExecution.Execution;
//using AElf.Kernel.ChainController.Application;
//using AElf.Kernel.SmartContract.Domain;
//using AElf.Kernel.SmartContractExecution.Application;
//using AElf.Kernel.SmartContractExecution.Domain;
//using AElf.SmartContract.Contexts;
//using Volo.Abp.DependencyInjection;
//
//namespace AElf.Contracts.Genesis.Tests
//{
//    public class MockSetup : ITransientDependency
//    {
//        // IncrementId is used to differentiate txn
//        // which is identified by From/To/IncrementId
//        private static int _incrementId;
//        public ulong NewIncrementId()
//        {
//            var n = Interlocked.Increment(ref _incrementId);
//            return (ulong)n;
//        }
//
//        public IStateManager StateManager { get; }
//        public int ChainId1 { get; } = ChainHelpers.GetChainId(123);
//        public ISmartContractManager SmartContractManager;
//        public ISmartContractService SmartContractService;
//
//        public IChainContextService ChainContextService;
//
//        public IAccountDataProvider DataProvider1;
//
//        public ServicePack ServicePack;
//
//        private IChainCreationService _chainCreationService;
//
//        private ISmartContractRunnerContainer _smartContractRunnerContainer;
//
//        public MockSetup(IStateManager stateManager,
//            IChainCreationService chainCreationService,
//            IChainContextService chainContextService,
//            ISmartContractRunnerContainer smartContractRunnerContainer,
//            ISmartContractManager smartContractManager,
//            ISmartContractService smartContractService)
//        {
//            StateManager = stateManager;
//            _chainCreationService = chainCreationService;
//            ChainContextService = chainContextService;
//            _smartContractRunnerContainer = smartContractRunnerContainer;
//            SmartContractManager = smartContractManager;
//            Task.Factory.StartNew(async () => { await Init(); }).Unwrap().Wait();
//            SmartContractService = smartContractService;
//
//            ServicePack = new ServicePack()
//            {
//                ChainContextService = chainContextService,
//                SmartContractService = SmartContractService,
//                ResourceDetectionService = null,
//                StateManager = stateManager
//            };
//         }
//
//        private byte[] SmartContractZeroCode
//        {
//            get
//            {
//                var filePath = Path.GetFullPath("../../../../AElf.Contracts.Genesis/bin/Debug/netstandard2.0/AElf.Contracts.Genesis.dll");
//                return File.ReadAllBytes(filePath);
//            }
//        }
//        
//        public byte[] AuthorizationCode
//        {
//            get
//            {
//                var filePath = Path.GetFullPath("../../../../AElf.Contracts.Authorization/bin/Debug/netstandard2.0/AElf.Contracts.Authorization.dll");
//                return File.ReadAllBytes(filePath);
//            }
//        }
//        
//        private async Task Init()
//        {
//            var reg0 = new SmartContractRegistration
//            {
//                Category = 0,
//                ContractBytes = ByteString.CopyFrom(SmartContractZeroCode),
//                ContractHash = Hash.FromRawBytes(SmartContractZeroCode),
//                SerialNumber = GlobalConfig.GenesisBasicContract
//            };
//            
//            var reg1 = new SmartContractRegistration
//            {
//                Category = 0,
//                ContractBytes = ByteString.CopyFrom(AuthorizationCode),
//                ContractHash = Hash.FromRawBytes(AuthorizationCode),
//                SerialNumber = GlobalConfig.AuthorizationContract
//            };
//
//            var chain1 = await _chainCreationService.CreateNewChainAsync(ChainId1, new
//                List<SmartContractRegistration> {reg0, reg1});
//        }
//        
//        public async Task<IExecutive> GetExecutiveAsync(Address address)
//        {
//            var executive = await SmartContractService.GetExecutiveAsync(address, ChainId1);
//            return executive;
//        }
//    }
//}
