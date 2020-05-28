using AElf.Kernel.Token.Infrastructure;
using AElf.Modularity;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.Modularity;

namespace AElf.Kernel.Token
{
    public class TokenKernelAElfModule : AElfModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            context.Services.AddSingleton<IPrimaryTokenSymbolProvider, PrimaryTokenSymbolProvider>();
            context.Services.AddTransient<IPrimaryTokenSymbolService, PrimaryTokenSymbolService>();
        }
    }
}