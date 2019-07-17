using AElf.Contracts.TestKit;
using Volo.Abp;

// ReSharper disable InconsistentNaming
namespace AElf.Contracts.TestKet.AEDPoSExtension
{
    public class AEDPoSExtensionTestBase : AEDPoSExtensionTestBase<ContractTestModule>
    {
    }
    
    public class AEDPoSExtensionTestBase<TModule> : AbpIntegratedTest<TModule>
        where TModule : ContractTestModule
    {
        
    }
}