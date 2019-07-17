using AElf.Contracts.TestKit;
using Microsoft.Extensions.DependencyInjection;

// ReSharper disable InconsistentNaming
namespace AElf.Contracts.TestKet.AEDPoSExtension
{
    public class AEDPoSExtensionTestBase : ContractTestBase<ContractTestAEDPoSExtensionModule>
    {
        protected IBlockMiningService BlockMiningService =>
            Application.ServiceProvider.GetRequiredService<IBlockMiningService>();
    }
}