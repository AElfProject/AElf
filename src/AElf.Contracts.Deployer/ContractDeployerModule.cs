using AElf.CSharp.CodeOps;
using AElf.Modularity;
using Volo.Abp.Modularity;

namespace AElf.Contracts.Deployer
{
    [DependsOn(typeof(CSharpCodeOpsAElfModule))]
    public class ContractDeployerModule : AElfModule

    {
    }
}