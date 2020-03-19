using AElf.Modularity;
using Volo.Abp.Modularity;

namespace AElf.Kernel.FeeCalculation
{
    [DependsOn(
        typeof(FeeCalculationModule),
        typeof(TestBaseKernelAElfModule))]
    public class KernelTransactionFeeTestAElfModule : AElfModule
    {

    }
}