using System.Runtime.CompilerServices;
using AElf.Kernel.Blockchain.Application;
using AElf.Kernel.Miner.Application;
using AElf.Kernel.SmartContract.Application;
using AElf.Kernel.SmartContract.Infrastructure;
using AElf.Modularity;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp;
using Volo.Abp.Modularity;
using Volo.Abp.Threading;

[assembly: InternalsVisibleTo("AElf.Kernel.SmartContract.Tests")]
namespace AElf.Kernel.SmartContract
{
    [DependsOn(typeof(CoreKernelAElfModule))]
    public class SmartContractAElfModule: AElfModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            context.Services.AddSingleton<ISmartContractRunnerContainer, SmartContractRunnerContainer>();
            context.Services.AddSingleton<ITransactionSizeFeeUnitPriceProvider, DefaultTransactionSizeFeeUnitPriceProvider>();
        }

        public override void OnApplicationInitialization(ApplicationInitializationContext context)
        {
            var smartContractExecutiveProvider = context.ServiceProvider.GetService<ISmartContractExecutiveProvider>();
            var blockchainService = context.ServiceProvider.GetService<IBlockchainService>();
            var chain = AsyncHelper.RunSync(() => blockchainService.GetChainAsync());
            if (chain == null) return;
            smartContractExecutiveProvider.Init(chain.LastIrreversibleBlockHash, chain.LastIrreversibleBlockHeight);
        }
        
        public override void OnPostApplicationInitialization(ApplicationInitializationContext context)
        {
            var deployedContractAddressService = context.ServiceProvider.GetService<IDeployedContractAddressService>();
            AsyncHelper.RunSync(() => deployedContractAddressService.InitAsync());
        }
    }
}