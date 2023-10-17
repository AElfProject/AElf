using AElf.Contracts.MultiToken;
using AElf.Kernel.FeeCalculation.Infrastructure;
using Volo.Abp.DependencyInjection;

namespace AElf.Runtime.WebAssembly.TransactionPayment;

public class FeeFunctionProvider : IFeeFunctionProvider, ISingletonDependency
{
    public CalculateFunction GetFunction()
    {
        var function = new CalculateFunction((int)FeeTypeEnum.Tx);
        function.AddFunction(new[] { 1 }, i => i * WebAssemblyTransactionPaymentConstants.FeeWeightRatio);
        return function;
    }
}