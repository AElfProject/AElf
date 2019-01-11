using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using AElf.Kernel;
using AElf.ChainController;
using AElf.SmartContract;
using AElf.Kernel.Managers;
using Google.Protobuf;
using AElf.Common;
using AElf.Execution.Execution;

namespace AElf.Contracts.Genesis.Tests
{
    public class MockSetup
    {
        // IncrementId is used to differentiate txn
        // which is identified by From/To/IncrementId
        private static int _incrementId;
        public ulong NewIncrementId()
        {
            var n = Interlocked.Increment(ref _incrementId);
            return (ulong)n;
        }

        public IStateManager StateManager { get; }
        public Hash ChainId1 { get; } = Hash.LoadByteArray(new byte[] { 0x01, 0x02, 0x03 });
        public ISmartContractManager SmartContractManager;
        public ISmartContractService SmartContractService;
        private IFunctionMetadataService _functionMetadataService;

        public IChainContextService ChainContextService;

        public IAccountDataProvider DataProvider1;

        public ServicePack ServicePack;

        private IChainCreationService _chainCreationService;

        private ISmartContractRunnerContainer _smartContractRunnerContainer;

        public MockSetup(IStateManager stateManager, IChainCreationService chainCreationService,
            IChainContextService chainContextService,
            IFunctionMetadataService functionMetadataService, ISmartContractRunnerContainer smartContractRunnerContainer,
            ISmartContractManager smartContractManager, IChainService chainService)
        {
            StateManager = stateManager;
            _chainCreationService = chainCreationService;
            ChainContextService = chainContextService;
            _functionMetadataService = functionMetadataService;
            _smartContractRunnerContainer = smartContractRunnerContainer;
            SmartContractManager = smartContractManager;
            Task.Factory.StartNew(async () => { await Init(); }).Unwrap().Wait();
            SmartContractService = new SmartContractService(SmartContractManager, _smartContractRunnerContainer,
                StateManager, _functionMetadataService, chainService);

            ServicePack = new ServicePack()
            {
                ChainContextService = chainContextService,
                SmartContractService = SmartContractService,
                ResourceDetectionService = null,
                StateManager = stateManager
            };
         }

        private byte[] SmartContractZeroCode
        {
            get
            {
                var filePath = Path.GetFullPath("../../../../AElf.Contracts.Genesis/bin/Debug/netstandard2.0/AElf.Contracts.Genesis.dll");
                return File.ReadAllBytes(filePath);
            }
        }
        
        public byte[] AuthorizationCode
        {
            get
            {
                var filePath = Path.GetFullPath("../../../../AElf.Contracts.Authorization/bin/Debug/netstandard2.0/AElf.Contracts.Authorization.dll");
                return File.ReadAllBytes(filePath);
            }
        }
        
        private async Task Init()
        {
            var reg0 = new SmartContractRegistration
            {
                Category = 0,
                ContractBytes = ByteString.CopyFrom(SmartContractZeroCode),
                ContractHash = Hash.FromRawBytes(SmartContractZeroCode),
                SerialNumber = GlobalConfig.GenesisBasicContract
            };
            
            var reg1 = new SmartContractRegistration
            {
                Category = 0,
                ContractBytes = ByteString.CopyFrom(AuthorizationCode),
                ContractHash = Hash.FromRawBytes(AuthorizationCode),
                SerialNumber = GlobalConfig.AuthorizationContract
            };

            var chain1 = await _chainCreationService.CreateNewChainAsync(ChainId1, new
                List<SmartContractRegistration> {reg0, reg1});
        }
        
        public async Task<IExecutive> GetExecutiveAsync(Address address)
        {
            var executive = await SmartContractService.GetExecutiveAsync(address, ChainId1);
            return executive;
        }
    }
}
