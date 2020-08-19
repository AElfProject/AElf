using AElf.Modularity;
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