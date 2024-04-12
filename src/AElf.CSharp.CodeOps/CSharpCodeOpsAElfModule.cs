using AElf.CSharp.CodeOps.Patchers;
using AElf.CSharp.CodeOps.Patchers.Module;
using AElf.CSharp.CodeOps.Policies;
using AElf.CSharp.CodeOps.Validators.Assembly;
using AElf.CSharp.CodeOps.Validators.Whitelist;
using AElf.Kernel.CodeCheck;
using AElf.Kernel.CodeCheck.Infrastructure;
using AElf.Modularity;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.Modularity;

namespace AElf.CSharp.CodeOps;

[DependsOn(typeof(CodeCheckAElfModule))]
public class CSharpCodeOpsAElfModule : AElfModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        context.Services.AddSingleton<IContractAuditor, CSharpContractAuditor>();
        context.Services.AddSingleton<IContractPatcher, CSharpContractPatcher>();
        var configuration = context.Services.GetConfiguration();
        Configure<CSharpCodeOpsOptions>(configuration.GetSection("CSharpCodeOps"));

        context.Services.AddSingleton<IPolicy, DefaultPolicy>();
        context.Services.AddSingleton<IAcsValidator, AcsValidator>();
        context.Services.AddSingleton<IWhitelistProvider, WhitelistProvider>();

        context.Services.AddTransient<IPatcher, StateWrittenSizeLimitMethodInjector>();
        context.Services.AddTransient<IPatcher, ResetFieldsMethodInjector>();
        context.Services.AddTransient<IPatcher, Patchers.Module.CallAndBranchCounts.Patcher>();
        context.Services.AddTransient<IPatcher, Patchers.Module.SafeMethods.StringMethodsReplacer>();
        context.Services.AddTransient<IPatcher, Patchers.Module.SafeMath.Patcher>();
    }
}