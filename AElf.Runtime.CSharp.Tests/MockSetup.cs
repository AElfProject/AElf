using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Reflection;
using AElf.Kernel;
using AElf.Kernel.Storages;
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

namespace AElf.Runtime.CSharp.Tests
{
    public class MockSetup
    {
        private static int _incrementId = 0;
        public ulong NewIncrementId()
        {
            var n = Interlocked.Increment(ref _incrementId);
            return (ulong)n;
        }

        public Hash ChainId1 { get; } = Hash.Generate();
        public Hash ChainId2 { get; } = Hash.Generate();
        public ISmartContractService SmartContractService;

        public IAccountDataProvider DataProvider1;
        public IAccountDataProvider DataProvider2;

        public Hash ContractAddress1 { get; } = Hash.Generate();
        public Hash ContractAddress2 { get; } = Hash.Generate();

        private ISmartContractManager _smartContractManager;
        public IWorldStateDictator WorldStateDictator;
        private IChainCreationService _chainCreationService;
        private IBlockManager _blockManager;
        private IFunctionMetadataService _functionMetadataService;

        private ISmartContractRunnerFactory _smartContractRunnerFactory;

        public MockSetup(IWorldStateDictator worldStateDictator, IChainCreationService chainCreationService, IBlockManager blockManager, SmartContractStore smartContractStore, IFunctionMetadataService functionMetadataService, ISmartContractRunnerFactory smartContractRunnerFactory)
        {
            WorldStateDictator = worldStateDictator;
            _chainCreationService = chainCreationService;
            _blockManager = blockManager;
            _functionMetadataService = functionMetadataService;
            _smartContractRunnerFactory = smartContractRunnerFactory;
            _smartContractManager = new SmartContractManager(smartContractStore);
            Task.Factory.StartNew(async () =>
            {
                await Init();
            }).Unwrap().Wait();
            SmartContractService = new SmartContractService(_smartContractManager, _smartContractRunnerFactory, WorldStateDictator, _functionMetadataService);
            Task.Factory.StartNew(async () =>
            {
                await DeploySampleContracts();
            }).Unwrap().Wait();
        }
        
        public byte[] SmartContractZeroCode
        {
            get
            {
                return ContractCodes.TestContractZeroCode;
            }
        }

        private async Task Init()
        {
            var reg = new SmartContractRegistration
            {
                Category = 0,
                ContractBytes = ByteString.CopyFrom(SmartContractZeroCode),
                ContractHash = Hash.Zero,
                Type = (int)SmartContractType.BasicContractZero
            };
            var chain1 = await _chainCreationService.CreateNewChainAsync(ChainId1, new List<SmartContractRegistration>{reg});
            var genesis1 = await _blockManager.GetBlockAsync(chain1.GenesisBlockHash);
            DataProvider1 = await (WorldStateDictator.SetChainId(ChainId1)).GetAccountDataProvider(ResourcePath.CalculatePointerForAccountZero(ChainId1));

            var chain2 = await _chainCreationService.CreateNewChainAsync(ChainId2, new List<SmartContractRegistration>{reg});
            var genesis2 = await _blockManager.GetBlockAsync(chain2.GenesisBlockHash);
            DataProvider2 = await (WorldStateDictator.SetChainId(ChainId2)).GetAccountDataProvider(ResourcePath.CalculatePointerForAccountZero(ChainId2));
        }

        private async Task DeploySampleContracts()
        {
            var reg = new SmartContractRegistration
            {
                Category = 1,
                ContractBytes = ByteString.CopyFrom(ContractCode),
                ContractHash = new Hash(ContractCode)
            };

            await SmartContractService.DeployContractAsync(ChainId1, ContractAddress1, reg, false);
            await SmartContractService.DeployContractAsync(ChainId2, ContractAddress2, reg, false);
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
                using (FileStream file = File.OpenRead(System.IO.Path.GetFullPath($"{SdkDir}/AElf.Runtime.CSharp.Tests.TestContract.dll")))
                {
                    code = file.ReadFully();
                }
                return code;
            }
        }
    }
}
