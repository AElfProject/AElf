using AElf.Kernel.FeeCalculation;
using AElf.Modularity;
using Volo.Abp.Modularity;

namespace AElf.Runtime.WebAssembly.TransactionPayment;

[DependsOn(typeof(FeeCalculationModule))]
public class WebAssemblyRuntimeAElfModule : AElfModule
{
}