using AElf.Modularity;
using Volo.Abp.Modularity;

namespace AElf.ContractDeployer
{
    [DependsOn(typeof(ContractDeployerModule))]
    public class ContractDeployerTestAElfModule : AElfModule
    {
    }
}