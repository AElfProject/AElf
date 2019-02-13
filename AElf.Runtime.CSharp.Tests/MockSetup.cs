using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using AElf.Kernel;
using AElf.Kernel.Managers;
using AElf.ChainController;
using AElf.SmartContract;
using AElf.Kernel.Tests;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Xunit;
using AElf.Runtime.CSharp;
using Xunit.Frameworks.Autofac;
using AElf.Common;
using AElf.SmartContract.Contexts;
using Volo.Abp.DependencyInjection;

namespace AElf.Runtime.CSharp.Tests
{
    public class MockSetup : ITransientDependency
    {
        private static int _incrementId = 0;
        public ulong NewIncrementId()
        {
            var n = Interlocked.Increment(ref _incrementId);
            return (ulong) n;
        }

        public int ChainId1 { get; } = ChainHelpers.GetChainId(123);
        public int ChainId2 { get; } = ChainHelpers.GetChainId(124);
        public ISmartContractService SmartContractService;

        public IStateProviderFactory StateProviderFactory;
        public IStateManager StateManager;
        public DataProvider DataProvider1;
        public DataProvider DataProvider2;

        public Address ContractAddress1;
        public Address ContractAddress2;

        private IChainCreationService _chainCreationService;
        public IChainService ChainService;

        public MockSetup(IStateManager stateManager,
            IStateProviderFactory stateProviderFactory,
            IChainCreationService chainCreationService,
            IChainService chainService,
            ISmartContractService smartContractService)
        {
            StateProviderFactory = stateProviderFactory;
            StateManager = stateManager;
            _chainCreationService = chainCreationService;
            ChainService = chainService;
            SmartContractService = smartContractService;
            Task.Factory.StartNew(async () =>
            {
                await Init();
            }).Unwrap().Wait();
            Task.Factory.StartNew(async () =>
            {
                await DeploySampleContracts();
            }).Unwrap().Wait();
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
                ContractHash = Hash.Zero,
                SerialNumber = GlobalConfig.GenesisBasicContract
            };

            var chain1 =
                await _chainCreationService.CreateNewChainAsync(ChainId1, new List<SmartContractRegistration> {reg});
            DataProvider1 = DataProvider.GetRootDataProvider(
                chain1.Id,
                Address.Generate()
            );
            DataProvider1.StateManager = StateManager;

            var chain2 =
                await _chainCreationService.CreateNewChainAsync(ChainId2, new List<SmartContractRegistration> {reg});
            DataProvider2 = DataProvider.GetRootDataProvider(
                chain2.Id,
                Address.Generate()
            );
            DataProvider2.StateManager = StateManager;
        }

        private async Task DeploySampleContracts()
        {
            const ulong serialNumber = 10ul;
            var reg = new SmartContractRegistration
            {
                Category = 1,
                ContractBytes = ByteString.CopyFrom(ContractCode),
                ContractHash = Hash.FromRawBytes(ContractCode),
                SerialNumber = serialNumber
            };

            await SmartContractService.DeploySystemContractAsync(ChainId1, reg);
            await SmartContractService.DeploySystemContractAsync(ChainId2, reg);
            
            ContractAddress1 = Address.BuildContractAddress(ChainId1, serialNumber);
            ContractAddress2 = Address.BuildContractAddress(ChainId2, serialNumber);
        }

        public string SdkDir => "../../../../AElf.Runtime.CSharp.Tests.TestContract/bin/Debug/netstandard2.0";

        public byte[] ContractCode => File.ReadAllBytes(Path.GetFullPath($"{SdkDir}/AElf.Runtime.CSharp.Tests.TestContract.dll"));
    }
}