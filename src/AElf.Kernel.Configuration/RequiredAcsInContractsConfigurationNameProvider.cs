using Volo.Abp.DependencyInjection;

namespace AElf.Kernel.Configuration
{
    public class RequiredAcsInContractsConfigurationNameProvider : IConfigurationNameProvider, ISingletonDependency
    {
        public static readonly string Name = "RequiredAcsInContracts";
        public string ConfigurationName => Name;
    }
}