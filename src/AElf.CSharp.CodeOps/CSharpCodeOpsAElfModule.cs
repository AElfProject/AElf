using AElf.CSharp.CodeOps.Policies;
using AElf.CSharp.CodeOps.Validators;
using AElf.CSharp.CodeOps.Validators.Assembly;
using AElf.CSharp.CodeOps.Validators.Method;
using AElf.CSharp.CodeOps.Validators.Whitelist;
using AElf.Kernel.CodeCheck;
using AElf.Kernel.CodeCheck.Infrastructure;
using AElf.Modularity;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.Modularity;

namespace AElf.CSharp.CodeOps
{
    [DependsOn(typeof(CodeCheckAElfModule))]
    public class CSharpCodeOpsAElfModule : AElfModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            context.Services.AddSingleton<IContractAuditor, CSharpContractAuditor>();
            var configuration = context.Services.GetConfiguration();
            Configure<CSharpCodeOpsOptions>(configuration.GetSection("CSharpCodeOps"));

            context.Services.AddSingleton<IAuditPolicy, DefaultAuditPolicy>();
            context.Services.AddSingleton<IAcsValidator, AcsValidator>();
            context.Services.AddSingleton<IWhitelistProvider, WhitelistProvider>();
        }
    }
}