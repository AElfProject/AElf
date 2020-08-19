using AElf.Modularity;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Volo.Abp.Modularity;

namespace AElf.Kernel.Token.Test
{
    [DependsOn(
        typeof(TokenKernelAElfModule), 
        typeof(KernelTestAElfModule))]
    public class KernelTokenModule : AElfModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
        }
    }
}