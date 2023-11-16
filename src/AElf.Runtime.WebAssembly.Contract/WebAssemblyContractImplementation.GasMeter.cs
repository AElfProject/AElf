using AElf.Runtime.WebAssembly.TransactionPayment;
using AElf.Runtime.WebAssembly.TransactionPayment.Extensions;

namespace AElf.Runtime.WebAssembly.Contract;

public partial class WebAssemblyContractImplementation
{
    public bool EstimateGas { get; set; }

    public Weight? ChargeGas(RuntimeCost runtimeCost)
    {
        var gasLeft = GasMeter?.ChargeGas(runtimeCost);
        if (gasLeft != null && !EstimateGas && gasLeft.Insufficient())
        {
            HandleError(WebAssemblyError.OutOfGas);
        }

        return gasLeft;
    }
}