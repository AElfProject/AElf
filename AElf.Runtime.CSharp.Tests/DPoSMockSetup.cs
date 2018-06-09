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

        private readonly ISmartContractRunnerFactory _smartContractRunnerFactory 
            = new SmartContractRunnerFactory();

        public IAccountDataProvider AccountDataProviderOfAccountZero;

        public DPoSMockSetup(ISmartContractStore smartContractStore, IWorldStateManager worldStateManager)
        {
            ISmartContractManager smartContractManager = new SmartContractManager(smartContractStore);
            
            var runner = new SmartContractRunner("../../../../AElf.Contracts.DPoS/bin/Debug/netstandard2.0/");
            _smartContractRunnerFactory.AddRunner(0, runner);
            
            AccountDataProviderOfAccountZero = worldStateManager.OfChain(ChainId).Result
                .GetAccountDataProvider(Path.CalculatePointerForAccountZero(ChainId));
            
            SmartContractService = new SmartContractService(smartContractManager,
                _smartContractRunnerFactory, worldStateManager);

            Task.Factory.StartNew(async () =>
            {
                await DeployDPoSContracts();
            }).Unwrap().Wait();
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