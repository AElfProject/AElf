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
        }
    }
}