using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Kernel.SmartContract;
using AElf.Modularity;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Volo.Abp.Modularity;

namespace AElf.Kernel.SmartContractExecution
{
    [DependsOn(
        typeof(SmartContractExecutionAElfModule),
        typeof(KernelCoreTestAElfModule),
        typeof(TransactionExecutingDependencyTestModule)
    )]
    public class SmartContractExecutionTestAElfModule : AElfModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
        }
    }
}