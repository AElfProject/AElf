using AElf.Kernel.FeeCalculation.Infrastructure;

namespace AElf.Runtime.WebAssembly.TransactionPayment;

public interface IFeeFunctionProvider
{
    CalculateFunction GetFunction();
    //Task SetFunctionAsync(CalculateFeeCoefficients coefficients);
}