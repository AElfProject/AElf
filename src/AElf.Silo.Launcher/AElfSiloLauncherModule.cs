using AElf.CrossChain;
using AElf.CSharp.CodeOps;
using AElf.EconomicSystem;
using AElf.GovernmentSystem;
using AElf.Kernel;
using AElf.Kernel.Configuration;
using AElf.Kernel.Consensus;
using AElf.Kernel.Proposal;
using AElf.Kernel.SmartContract;
using AElf.Kernel.SmartContract.ExecutionPluginForCallThreshold;
using AElf.Kernel.SmartContract.ExecutionPluginForMethodFee;
using AElf.Kernel.SmartContract.ExecutionPluginForResourceFee;
using AElf.Kernel.SmartContract.Grains;
using AElf.Kernel.Token;
using AElf.RuntimeSetup;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.AspNetCore;
using Volo.Abp.Autofac;
using Volo.Abp.Modularity;

namespace AElf.Silo.Launcher;

[DependsOn(typeof(AbpAutofacModule),
    typeof(ExecutionPluginForMethodFeeModule),
    typeof(ExecutionPluginForResourceFeeModule),
    typeof(ExecutionPluginForCallThresholdModule),
    typeof(CrossChainCoreModule),
    typeof(ConfigurationAElfModule),
    typeof(ProposalAElfModule),
    typeof(TokenKernelAElfModule),
    typeof(CSharpCodeOpsAElfModule),
    typeof(RuntimeSetupAElfModule),
    typeof(GovernmentSystemAElfModule),
    typeof(EconomicSystemAElfModule),
    typeof(GrainExecutionAElfModule),
    typeof(CoreConsensusAElfModule),
    typeof(AbpAspNetCoreModule)
)]
public class AElfSiloLauncherModule : AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        context.Services.AddHostedService<SiloTransactionExecutingHost>();
    }

    public override void PreConfigureServices(ServiceConfigurationContext context)
    {
        var configuration = context.Services.GetConfiguration();
        var hostingEnvironment = context.Services.GetHostingEnvironment();
        var contentRootPath = hostingEnvironment.ContentRootPath;
        var chainType = configuration.GetValue("ChainType", ChainType.MainChain);
        var netType = configuration.GetValue("NetType", NetType.MainNet);
        var newConfig = new ConfigurationBuilder().AddConfiguration(configuration)
            .AddJsonFile($"appsettings.{chainType}.{netType}.json")
            .AddJsonFile($"appsettings.{hostingEnvironment.EnvironmentName}.json", true)
            .SetBasePath(contentRootPath)
            .Build();
        Configure<ChainOptions>(option =>
        {
            option.ChainId = ChainHelper.ConvertBase58ToChainId(newConfig["ChainId"]);
            option.ChainType = chainType;
            option.NetType = netType;
        });

        Configure<HostSmartContractBridgeContextOptions>(options =>
        {
            options.ContextVariables[ContextVariableDictionary.NativeSymbolName] =
                newConfig.GetValue("Economic:Symbol", "ELF");
            options.ContextVariables["SymbolListToPayTxFee"] =
                newConfig.GetValue("Economic:SymbolListToPayTxFee", "WRITE,READ,STORAGE,TRAFFIC");
            options.ContextVariables["SymbolListToPayRental"] =
                newConfig.GetValue("Economic:SymbolListToPayRental", "CPU,RAM,DISK,NET");
        });
    }
}