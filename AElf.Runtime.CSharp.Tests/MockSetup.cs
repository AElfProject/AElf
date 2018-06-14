﻿using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Reflection;
using AElf.Kernel;
using AElf.Kernel.Storages;
using AElf.Kernel.Extensions;
using AElf.Kernel.KernelAccount;
using AElf.Kernel.Managers;
using AElf.Kernel.Services;
using AElf.Kernel.SmartContracts.CSharpSmartContract;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using ServiceStack;
using Xunit;
using AElf.Runtime.CSharp;
using Xunit.Frameworks.Autofac;
using Path = AElf.Kernel.Path;

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
        private IWorldStateManager _worldStateManager;
        private IChainCreationService _chainCreationService;
        private IBlockManager _blockManager;

        private ISmartContractRunnerFactory _smartContractRunnerFactory = new SmartContractRunnerFactory();

        public MockSetup(IWorldStateManager worldStateManager, IChainCreationService chainCreationService, IBlockManager blockManager, SmartContractStore smartContractStore)
        {
            _worldStateManager = worldStateManager;
            _chainCreationService = chainCreationService;
            _blockManager = blockManager;
            _smartContractManager = new SmartContractManager(smartContractStore);
            var runner = new SmartContractRunner("../../../../AElf.Runtime.CSharp.Tests.TestContract/bin/Debug/netstandard2.0/");
            _smartContractRunnerFactory.AddRunner(0, runner);
            Task.Factory.StartNew(async () =>
            {
                await Init();
            }).Unwrap().Wait();
            SmartContractService = new SmartContractService(_smartContractManager, _smartContractRunnerFactory, _worldStateManager);
            Task.Factory.StartNew(async () =>
            {
                await DeploySampleContracts();
            }).Unwrap().Wait();
        }

        private async Task Init()
        {
            var reg = new SmartContractRegistration
            {
                Category = 0,
                ContractBytes = ByteString.CopyFrom(new byte[] { }),
                ContractHash = Hash.Zero
            };
            var chain1 = await _chainCreationService.CreateNewChainAsync(ChainId1, reg);
            var genesis1 = await _blockManager.GetBlockAsync(chain1.GenesisBlockHash);
            DataProvider1 = (await _worldStateManager.OfChain(ChainId1)).GetAccountDataProvider(Path.CalculatePointerForAccountZero(ChainId1));

            var chain2 = await _chainCreationService.CreateNewChainAsync(ChainId2, reg);
            var genesis2 = await _blockManager.GetBlockAsync(chain2.GenesisBlockHash);
            DataProvider2 = (await _worldStateManager.OfChain(ChainId2)).GetAccountDataProvider(Path.CalculatePointerForAccountZero(ChainId2));
        }

        private async Task DeploySampleContracts()
        {
            var reg = new SmartContractRegistration
            {
                Category = 0,
                ContractBytes = ByteString.CopyFrom(ContractCode),
                ContractHash = new Hash(ContractCode)
            };

            await SmartContractService.DeployContractAsync(ContractAddress1, reg);
            await SmartContractService.DeployContractAsync(ContractAddress2, reg);
        }

        public byte[] ContractCode
        {
            get
            {
                byte[] code = null;
                using (FileStream file = File.OpenRead(System.IO.Path.GetFullPath("../../../../AElf.Runtime.CSharp.Tests.TestContract/bin/Debug/netstandard2.0/AElf.Runtime.CSharp.Tests.TestContract.dll")))
                {
                    code = file.ReadFully();
                }
                return code;
            }
        }
    }
}
