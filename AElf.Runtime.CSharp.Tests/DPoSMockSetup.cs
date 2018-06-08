using System.IO;
using System.Threading.Tasks;
using AElf.Kernel;
using AElf.Kernel.KernelAccount;
using AElf.Kernel.Managers;
using AElf.Kernel.Services;
using AElf.Kernel.Storages;
using Google.Protobuf;
using ServiceStack;
using Path = AElf.Kernel.Path;

namespace AElf.Runtime.CSharp.Tests
{
    // ReSharper disable once InconsistentNaming
    public class DPoSMockSetup
    {
        public Hash ChainId { get; } = Hash.Generate();

        public ISmartContractService SmartContractService;

        // ReSharper disable once InconsistentNaming
        public Hash DPoSContractAddress { get; } = Hash.Generate();
        
        private readonly ISmartContractManager _smartContractManager;
        private readonly IWorldStateManager _worldStateManager;
        
        private readonly ISmartContractRunnerFactory _smartContractRunnerFactory 
            = new SmartContractRunnerFactory();

        public IAccountDataProvider AccountDataProviderOfAccountZero;

        public DPoSMockSetup(ISmartContractStore smartContractStore, IWorldStateManager worldStateManager, 
            IChainCreationService chainCreationService, IBlockManager blockManager)
        {
            _smartContractManager = new SmartContractManager(smartContractStore);
            _worldStateManager = worldStateManager;
            _chainCreationService = chainCreationService;
            _blockManager = blockManager;
            
            var runner = new SmartContractRunner("../../../../AElf.Contracts.DPoS/bin/Debug/netstandard2.0/");
            _smartContractRunnerFactory.AddRunner(0, runner);
            
            Task.Factory.StartNew(async () =>
            {
                await Init();
            }).Unwrap().Wait();
            
            SmartContractService = new SmartContractService(_smartContractManager,
                _smartContractRunnerFactory, _worldStateManager);

            Task.Factory.StartNew(async () =>
            {
                await DeployDPoSContracts();
            }).Unwrap().Wait();
        }

        private async Task Init()
        {
            AccountDataProviderOfAccountZero = (await _worldStateManager.OfChain(ChainId))
                .GetAccountDataProvider(Path.CalculatePointerForAccountZero(ChainId));
        }

        // ReSharper disable once InconsistentNaming
        public byte[] DPoSContractCode
        {
            get
            {
                byte[] code;
                using (var file = File.OpenRead(
                    System.IO.Path.GetFullPath("../../../../AElf.Contracts.DPoS/bin/Debug/netstandard2.0/AElf.Contracts.DPoS.dll")))
                {
                    code = file.ReadFully();
                }
                return code;
            }
        }
        
        // ReSharper disable once InconsistentNaming
        private async Task DeployDPoSContracts()
        {
            var reg = new SmartContractRegistration
            {
                Category = 0,
                ContractBytes = ByteString.CopyFrom(DPoSContractCode),
                ContractHash = Hash.Zero
            };

            await SmartContractService.DeployContractAsync(DPoSContractAddress, reg);
        }
    }
}