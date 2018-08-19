using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Reflection;
using AElf.Kernel;
using AElf.Kernel.Storages;
using AElf.ChainController;
using AElf.SmartContract;
using AElf.Kernel.KernelAccount;
using AElf.Kernel.Managers;
using AElf.Execution;
using Google.Protobuf;
using ServiceStack;

namespace AElf.Contracts.Genesis.Tests
{
    public class MockSetup
    {
        // IncrementId is used to differentiate txn
        // which is identified by From/To/IncrementId
        private static int _incrementId = 0;
        public ulong NewIncrementId()
        {
            var n = Interlocked.Increment(ref _incrementId);
            return (ulong)n;
        }

        public Hash ChainId1 { get; } = Hash.Generate();
        public ISmartContractManager SmartContractManager;
        public ISmartContractService SmartContractService;
        private IFunctionMetadataService _functionMetadataService;

        public IChainContextService ChainContextService;

        public IAccountDataProvider DataProvider1;

        public ServicePack ServicePack;

        private IStateDictator _stateDictator;
        private IChainCreationService _chainCreationService;

        private ISmartContractRunnerFactory _smartContractRunnerFactory;

        public MockSetup(IStateDictator stateDictator, IChainCreationService chainCreationService,
            IDataStore dataStore, IChainContextService chainContextService,
            IFunctionMetadataService functionMetadataService, ISmartContractRunnerFactory smartContractRunnerFactory)
        {
            _stateDictator = stateDictator;
            _chainCreationService = chainCreationService;
            ChainContextService = chainContextService;
            _functionMetadataService = functionMetadataService;
            _smartContractRunnerFactory = smartContractRunnerFactory;
            SmartContractManager = new SmartContractManager(dataStore);
            Task.Factory.StartNew(async () => { await Init(); }).Unwrap().Wait();
            SmartContractService = new SmartContractService(SmartContractManager, _smartContractRunnerFactory,
                _stateDictator, _functionMetadataService);

            ServicePack = new ServicePack()
            {
                ChainContextService = chainContextService,
                SmartContractService = SmartContractService,
                ResourceDetectionService = null,
                StateDictator = _stateDictator
            };
        }

        public byte[] SmartContractZeroCode
        {
            get
            {
                byte[] code = null;
                using (FileStream file = File.OpenRead(System.IO.Path.GetFullPath("../../../../AElf.Contracts.Genesis/bin/Debug/netstandard2.0/AElf.Contracts.Genesis.dll")))
                {
                    code = file.ReadFully();
                }
                return code;
            }
        }
        
        private async Task Init()
        {
            var reg = new SmartContractRegistration
            {
                Category = 0,
                ContractBytes = ByteString.CopyFrom(SmartContractZeroCode),
                ContractHash = Hash.Zero
            };
            var chain1 = await _chainCreationService.CreateNewChainAsync(ChainId1, new List<SmartContractRegistration>{reg});
            _stateDictator.ChainId = ChainId1;
            DataProvider1 = _stateDictator.GetAccountDataProvider(ChainId1.SetHashType(HashType.AccountZero));
        }
        
        public async Task<IExecutive> GetExecutiveAsync(Hash address)
        {
            var executive = await SmartContractService.GetExecutiveAsync(address, ChainId1);
            return executive;
        }
    }
}
