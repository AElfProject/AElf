using AElf.Modularity;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.Modularity;

namespace AElf.Kernel.Token
{
    [DependsOn(typeof(CoreKernelAElfModule))]
    public class TokenKernelAElfModule: AElfModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            context.Services.AddAssemblyOf<TokenKernelAElfModule>();
        }
    }
}