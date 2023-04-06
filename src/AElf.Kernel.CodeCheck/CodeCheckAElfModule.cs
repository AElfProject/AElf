using AElf.Kernel.Blockchain.Application;
using AElf.Kernel.CodeCheck.Application;
using AElf.Kernel.CodeCheck.Infrastructure;
using AElf.Kernel.Configuration;
using AElf.Kernel.Miner.Application;
using AElf.Kernel.SmartContract.Application;
using AElf.Kernel.Txn.Application;
using AElf.Modularity;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.Modularity;

namespace AElf.Kernel.CodeCheck;

[DependsOn(typeof(ConfigurationAElfModule))]
public class CodeCheckAElfModule : AElfModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        var configuration = context.Services.GetConfiguration();
        Configure<CodeCheckOptions>(configuration.GetSection("CodeCheck"));

        context.Services
            .AddSingleton<IBlocksExecutionSucceededLogEventProcessor, CodeCheckRequiredLogEventProcessor>();
        //context.Services.AddSingleton<IBlockAcceptedLogEventProcessor, ContractDeployedLogEventProcessor>();
        context.Services.AddSingleton<IContractAuditorContainer, ContractAuditorContainer>();
        context.Services.AddSingleton<IBlockValidationProvider, CodeCheckValidationProvider>();
        context.Services.AddTransient<ISystemTransactionGenerator, CodeCheckProposalReleaseTransactionGenerator>();
    }
}