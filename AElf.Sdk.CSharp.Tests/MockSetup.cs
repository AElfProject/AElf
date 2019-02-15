using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AElf.Kernel;
using AElf.Kernel.ChainController;
using AElf.SmartContract;
using AElf.Kernel.SmartContractExecution;
using Google.Protobuf;
using AElf.Kernel.Tests;
using AElf.Common;
using AElf.Kernel.SmartContractExecution.Execution;
using AElf.Kernel.ChainController.Application;
using AElf.Kernel.Managers;
using AElf.Kernel.SmartContract.Domain;
using AElf.Kernel.SmartContractExecution.Application;
using AElf.Kernel.SmartContractExecution.Domain;
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

        public int ChainId1 { get; } = ChainHelpers.GetChainId(123);
        public ISmartContractManager SmartContractManager;
        public ISmartContractService SmartContractService;

        public IChainContextService ChainContextService;

        public IStateManager StateManager;
        public DataProvider DataProvider1;

        public ServicePack ServicePack;

        private IChainCreationService _chainCreationService;

        public MockSetup(IStateManager stateManager,
            IChainCreationService chainCreationService,
            IChainContextService chainContextService,
            ISmartContractManager smartContractManager,
            ISmartContractService smartContractService)
        {
            StateManager = stateManager;
            _chainCreationService = chainCreationService;
            ChainContextService = chainContextService;
            SmartContractManager = smartContractManager;
            Task.Factory.StartNew(async () =>
            {
                await Init();
            }).Unwrap().Wait();
            SmartContractService = smartContractService;
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