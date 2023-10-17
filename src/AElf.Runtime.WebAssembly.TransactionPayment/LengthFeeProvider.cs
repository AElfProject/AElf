using Volo.Abp.DependencyInjection;

namespace AElf.Runtime.WebAssembly.TransactionPayment;

public class LengthFeeProvider : IFeeProvider, ITransientDependency
{
    public long GetWeightFee(Weight weight)
    {
        return weight.ProofSize * WebAssemblyTransactionPaymentConstants.TransactionByteFee;
    }
}