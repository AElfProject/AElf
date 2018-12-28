using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Reflection;
using AElf.Kernel;
using AElf.Kernel.KernelAccount;
using AElf.Kernel.Managers;
using AElf.ChainController;
using AElf.SmartContract;
using AElf.Kernel.Tests;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using ServiceStack;
using Xunit;
using AElf.Runtime.CSharp;
using Xunit.Frameworks.Autofac;
using AElf.Common;

namespace AElf.Runtime.CSharp.Tests
{
    public class MockSetup
    {
        private static int _incrementId = 0;
        public ulong NewIncrementId()
        {
            var n = Interlocked.Increment(ref _incrementId);
            return (ulong) n;
        }

        public Hash ChainId1 { get; } = Hash.LoadByteArray(new byte[] {0x01, 0x02, 0x03});
        public Hash ChainId2 { get; } = Hash.LoadByteArray(new byte[] {0x01, 0x02, 0x04});
        public ISmartContractService SmartContractService;

        public IStateManager StateManager;
        public DataProvider DataProvider1;
        public DataProvider DataProvider2;

        public Address ContractAddress1;
        public Address ContractAddress2;

        private ISmartContractManager _smartContractManager;
        private IChainCreationService _chainCreationService;
        private IFunctionMetadataService _functionMetadataService;
        public IChainService ChainService;

        private ISmartContractRunnerContainer _smartContractRunnerContainer;

        public MockSetup(IStateManager stateManager, IChainCreationService chainCreationService,
            IFunctionMetadataService functionMetadataService, ISmartContractRunnerContainer smartContractRunnerContainer,
            ISmartContractManager smartContractManager, IChainService chainService)
        {
            StateManager = stateManager;
            _chainCreationService = chainCreationService;
            _functionMetadataService = functionMetadataService;
            _smartContractRunnerContainer = smartContractRunnerContainer;
            _smartContractManager = smartContractManager;
            ChainService = chainService;
            Task.Factory.StartNew(async () =>
            {
                await Init();
            }).Unwrap().Wait();
            SmartContractService = new SmartContractService(_smartContractManager, _smartContractRunnerContainer, StateManager, _functionMetadataService, ChainService);
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

        public string SdkDir
        {
            get => "../../../../AElf.Runtime.CSharp.Tests.TestContract/bin/Debug/netstandard2.0";
        }

        public byte[] ContractCode
        {
            get
            {
                byte[] code = null;
                using (FileStream file =
                    File.OpenRead(System.IO.Path.GetFullPath($"{SdkDir}/AElf.Runtime.CSharp.Tests.TestContract.dll")))
                {
                    code = file.ReadFully();
                }

                return code;
            }
        }
    }
}