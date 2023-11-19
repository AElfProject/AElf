using AElf.Kernel.SmartContract;
using AElf.Modularity;
using AElf.Runtime.CSharp;
using AElf.Runtime.WebAssembly.TransactionPayment;
using Volo.Abp.Modularity;

namespace AElf.Runtime.WebAssembly.Contract;

[DependsOn(
    typeof(SmartContractAElfModule),
    typeof(CSharpRuntimeAElfModule),
    typeof(WebAssemblyRuntimeTransactionPaymentAElfModule)
)]
public class WebAssemblyContractAElfModule : AElfModule
{
    
}