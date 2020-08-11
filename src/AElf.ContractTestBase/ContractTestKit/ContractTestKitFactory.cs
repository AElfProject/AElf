using Volo.Abp;
using Volo.Abp.Modularity;

namespace AElf.ContractTestBase.ContractTestKit
{
    public interface IContractTestKitFactory
    {
        ContractTestKit<TModule> Create<TModule>(IAbpApplication application) where TModule : AbpModule;
    }
    
    public class ContractTestKitFactory : IContractTestKitFactory
    {
        public ContractTestKit<TModule> Create<TModule>(IAbpApplication application) where TModule : AbpModule
        {
            return new ContractTestKit<TModule>(application);
        }
    }
}