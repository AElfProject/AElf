using AElf.Kernel.Configuration;
using Volo.Abp.DependencyInjection;

namespace AElf.Kernel.SmartContractExecution
{
    public class RequiredAcsInContractsConfigurationNameProvider : IConfigurationNameProvider, ISingletonDependency
    {
        public static readonly string Name = "RequiredAcsInContracts";
        public string ConfigurationName => Name;
    }
}