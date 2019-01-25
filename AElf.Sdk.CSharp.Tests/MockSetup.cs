using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AElf.Kernel;
using AElf.ChainController;
using AElf.SmartContract;
using AElf.Execution;
using Google.Protobuf;
using AElf.Kernel.Tests;
using AElf.Common;
using AElf.Execution.Execution;
using AElf.Kernel.Managers;
using AElf.SmartContract.Contexts;
using Volo.Abp.DependencyInjection;

namespace AElf.Sdk.CSharp.Tests
{
    public class MockSetup : ITransientDependency
    {
        // IncrementId is used to differentiate txn
        // which is identified by From/To/IncrementId
        private static int _incrementId = 0;

        public ulong NewIncrementId()
        {
            var n = Interlocked.Increment(ref _incrementId);
            return (ulong) n;
        }

        public int ChainId1 { get; } = Hash.LoadByteArray(new byte[] {0x01, 0x02, 0x03});
        public ISmartContractManager SmartContractManager;
        public ISmartContractService SmartContractService;
        private IFunctionMetadataService _functionMetadataService;

        public IChainContextService ChainContextService;

        public IStateManager StateManager;
        public DataProvider DataProvider1;

        public ServicePack ServicePack;

        private IChainCreationService _chainCreationService;
        private IChainService _chainService;

        private ISmartContractRunnerContainer _smartContractRunnerContainer;

        public MockSetup(IStateManager stateManager,
            IStateProviderFactory stateProviderFactory,
            IChainCreationService chainCreationService,
            IChainContextService chainContextService, IFunctionMetadataService functionMetadataService,
            ISmartContractRunnerContainer smartContractRunnerContainer, ISmartContractManager smartContractManager, IChainService chainService)
        {
            StateManager = stateManager;
            _chainCreationService = chainCreationService;
            ChainContextService = chainContextService;
            _functionMetadataService = functionMetadataService;
            _smartContractRunnerContainer = smartContractRunnerContainer;
            SmartContractManager = smartContractManager;
            _chainService = chainService;
            Task.Factory.StartNew(async () =>
            {
                await Init();
            }).Unwrap().Wait();
            SmartContractService = new SmartContractService(SmartContractManager, _smartContractRunnerContainer, stateProviderFactory, _functionMetadataService, chainService);

            ServicePack = new ServicePack()
            {
                ChainContextService = chainContextService,
                SmartContractService = SmartContractService,
                ResourceDetectionService = null,
                StateManager = StateManager
            };
        }

        public byte[] SmartContractZeroCode
        {
            get { return ContractCodes.TestContractZeroCode; }
        }

        private async Task Init()
        {
            var reg = new SmartContractRegistration
            {
                Category = 0,
                ContractBytes = ByteString.CopyFrom(SmartContractZeroCode),
                ContractHash = Hash.Zero
            };
            var chain1 =
                await _chainCreationService.CreateNewChainAsync(ChainId1, new List<SmartContractRegistration> {reg});

            DataProvider1 = DataProvider.GetRootDataProvider(
                chain1.Id,
                Address.Generate() // todo warning adr contract adress
            );
            DataProvider1.StateManager = StateManager;
        }

        public async Task<Address> DeployContractAsync(byte[] code)
        {
            const ulong serialNumber = 10ul;
            var reg = new SmartContractRegistration
            {
                Category = 1,
                ContractBytes = ByteString.CopyFrom(code),
                ContractHash = Hash.FromRawBytes(code),
                SerialNumber = serialNumber
            };

            await SmartContractService.DeploySystemContractAsync(ChainId1, reg);
            return Address.BuildContractAddress(ChainId1, serialNumber);
        }

        public async Task<IExecutive> GetExecutiveAsync(Address address)
        {
            var executive = await SmartContractService.GetExecutiveAsync(address, ChainId1);
            return executive;
        }
    }
}