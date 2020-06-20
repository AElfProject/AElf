using AElf.CSharp.CodeOps;
using AElf.Modularity;
using Volo.Abp.Modularity;

namespace AElf.ContractDeployer
{
    [DependsOn(typeof(CSharpCodeOpsAElfModule))]
    public class ContractDeployerModule : AElfModule

    {
    }
}