using System.Threading;
using System.Threading.Tasks;
using AElf.Kernel;
using AElf.Kernel.Concurrency.Execution;
using AElf.Kernel.Concurrency.Metadata;
using AElf.Kernel.KernelAccount;
using AElf.Kernel.Managers;
using AElf.Kernel.Services;
using AElf.Kernel.Storages;
using AElf.Runtime.CSharp;
using Google.Protobuf;

namespace AElf.Contracts.DPoS.Tests
{
    public class DPoSMockSetup
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

        public IAccountDataProvider DataProvider1;

        public ServicePack ServicePack;

        private IWorldStateDictator _worldStateDictator;
        private IChainCreationService _chainCreationService;
        private IBlockManager _blockManager;
        private IFunctionMetadataService _functionMetadataService;
        
        private ISmartContractRunnerFactory _smartContractRunnerFactory;

        public DPoSMockSetup(IWorldStateDictator worldStateDictator, IChainCreationService chainCreationService, 
            IBlockManager blockManager, ISmartContractStore smartContractStore, IChainContextService chainContextService,
            IFunctionMetadataService functionMetadataService, ISmartContractRunnerFactory smartContractRunnerFactory)
        {
            _worldStateDictator = worldStateDictator;
            _chainCreationService = chainCreationService;
            _blockManager = blockManager;
            _functionMetadataService = functionMetadataService;
            _smartContractRunnerFactory = smartContractRunnerFactory;
            SmartContractManager = new SmartContractManager(smartContractStore);
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

        private async Task Init()
        {
            DataProvider1 = await (_worldStateDictator.SetChainId(ChainId1)).GetAccountDataProvider(PathContext.CalculatePointerForAccountZero(ChainId1));
        }

        public async Task DeployContractAsync(byte[] code, Hash address)
        {
            var reg = new SmartContractRegistration
            {
                Category = 0,
                ContractBytes = ByteString.CopyFrom(code),
                ContractHash = new Hash(code)
            };

            await SmartContractService.DeployContractAsync(ChainId1, address, reg, true);
        }

        public async Task<IExecutive> GetExecutiveAsync(Hash address)
        {
            var executive = await SmartContractService.GetExecutiveAsync(address, ChainId1);
            return executive;
        }

    }
}
