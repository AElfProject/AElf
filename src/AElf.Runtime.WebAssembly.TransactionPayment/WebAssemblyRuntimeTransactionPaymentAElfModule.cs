using AElf.Kernel.FeeCalculation;
using AElf.Modularity;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.Modularity;

namespace AElf.Runtime.WebAssembly.TransactionPayment;

[DependsOn(typeof(FeeCalculationModule))]
public class WebAssemblyRuntimeTransactionPaymentAElfModule : AElfModule
{

}