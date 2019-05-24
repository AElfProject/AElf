using AElf.Kernel.Consensus.AEDPoS.Application;
using AElf.TestBase;
using Microsoft.Extensions.DependencyInjection;

namespace AElf.Kernel.Consensus.DPoS.Tests
{
    public class AEDPoSTestBase : AElfIntegratedTest<AEDPoSTestAElfModule>
    {
        protected IRandomHashCacheService RandomHashCacheService =>
            Application.ServiceProvider.GetRequiredService<IRandomHashCacheService>();
    }
}