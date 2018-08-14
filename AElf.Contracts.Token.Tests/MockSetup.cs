using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using AElf.Kernel;
using AElf.Kernel.Storages;
using AElf.Kernel.Managers;
using AElf.ChainController;
using AElf.SmartContract;
using AElf.Execution;
using Google.Protobuf;
using ServiceStack;

namespace AElf.Contracts.Token.Tests
{
    public class     MockSetup
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

        private IWorldStateDictator _worldStateDictator;
        private IChainCreationService _chainCreationService;

        private ISmartContractRunnerFactory _smartContractRunnerFactory;

        public MockSetup(IWorldStateDictator worldStateDictator, IChainCreationService chainCreationService, IDataStore dataStore, IChainContextService chainContextService, IFunctionMetadataService functionMetadataService, ISmartContractRunnerFactory smartContractRunnerFactory)
        {
            _worldStateDictator = worldStateDictator;
            _chainCreationService = chainCreationService;
            ChainContextService = chainContextService;
            _functionMetadataService = functionMetadataService;
            _smartContractRunnerFactory = smartContractRunnerFactory;
            SmartContractManager = new SmartContractManager(dataStore);
            Task.Factory.StartNew(async () =>
            {
                await Init();
            }).Unwrap().Wait();
            SmartContractService = new SmartContractService(SmartContractManager, _smartContractRunnerFactory, _worldStateDictator, _functionMetadataService);

            ServicePack = new ServicePack()
            {
                ChainContextService = chainContextService,
                SmartContractService = SmartContractService,
                ResourceDetectionService = null,
                WorldStateDictator = _worldStateDictator
            };
        }

        public byte[] TokenCode
        {
            get
            {
                byte[] code = null;
                using (FileStream file = File.OpenRead(Path.GetFullPath("../../../../AElf.Contracts.Token/bin/Debug/netstandard2.0/AElf.Contracts.Token.dll")))
                {
                    code = file.ReadFully();
                }
                return code;
            }
        }
        
        public byte[] SCZeroContractCode
        {
            get
            {
                byte[] code = null;
                using (FileStream file = File.OpenRead(Path.GetFullPath("../../../../AElf.Contracts.Genesis/bin/Debug/netstandard2.0/AElf.Contracts.Genesis.dll")))
                {
                    code = file.ReadFully();
                }
                return code;
            }
        }
        
        private async Task Init()
        {
            var reg1 = new SmartContractRegistration
            {
                Category = 0,
                ContractBytes = ByteString.CopyFrom(TokenCode),
                ContractHash = TokenCode.CalculateHash(),
                Type = (int)SmartContractType.TokenContract
            };
            var reg0 = new SmartContractRegistration
            {
                Category = 0,
                ContractBytes = ByteString.CopyFrom(SCZeroContractCode),
                ContractHash = SCZeroContractCode.CalculateHash(),
                Type = (int)SmartContractType.BasicContractZero
            };

            var chain1 =
                await _chainCreationService.CreateNewChainAsync(ChainId1,
                    new List<SmartContractRegistration> {reg0, reg1});
            DataProvider1 =
                await (_worldStateDictator.SetChainId(ChainId1)).GetAccountDataProvider(
                    ResourcePath.CalculatePointerForAccountZero(ChainId1));
        }
        
        public async Task<IExecutive> GetExecutiveAsync(Hash address)
        {
            var executive = await SmartContractService.GetExecutiveAsync(address, ChainId1);
            return executive;
        }
    }
}
