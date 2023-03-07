using AElf.CSharp.CodeOps;
using AElf.Modularity;
using Volo.Abp.Modularity;

namespace AElf.ContractDeployer;

[DependsOn(typeof(ContractDeployerModule),typeof(CSharpCodeOpsAElfModule))]
public class ContractDeployerTestAElfModule : AElfModule
{
}