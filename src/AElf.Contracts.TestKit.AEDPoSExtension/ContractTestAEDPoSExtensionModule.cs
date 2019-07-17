using AElf.Contracts.TestKit;
using AElf.Kernel.SmartContract.Application;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.Modularity;

// ReSharper disable InconsistentNaming
namespace AElf.Contracts.TestKet.AEDPoSExtension
{
    public class ContractTestAEDPoSExtensionModule<TSelf> : ContractTestAEDPoSExtensionModule
        where TSelf : ContractTestAEDPoSExtensionModule<TSelf>
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            context.Services.AddAssemblyOf<TSelf>();
            base.ConfigureServices(context);
        }
    }
    
    [DependsOn(
        typeof(ContractTestModule)
    )]
    public class ContractTestAEDPoSExtensionModule : ContractTestModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            context.Services.AddSingleton<ITransactionListProvider, TransactionListProvider>();
            context.Services.AddSingleton<IPostExecutionPlugin, ProvideTransactionListPostExecutionPlugin>();
        }
    }
}