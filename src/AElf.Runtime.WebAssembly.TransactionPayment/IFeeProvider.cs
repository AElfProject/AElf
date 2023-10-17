namespace AElf.Runtime.WebAssembly.TransactionPayment;

public interface IFeeProvider
{
    long GetWeightFee(Weight weight);
}