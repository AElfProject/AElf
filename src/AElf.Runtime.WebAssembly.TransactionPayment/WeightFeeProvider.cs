namespace AElf.Runtime.WebAssembly.TransactionPayment;

public class WeightFeeProvider : IFeeProvider
{
    private readonly IFeeFunctionProvider _feeFunctionProvider;

    public WeightFeeProvider(IFeeFunctionProvider feeFunctionProvider)
    {
        _feeFunctionProvider = feeFunctionProvider;
    }

    public long GetWeightFee(Weight weight)
    {
        var function = _feeFunctionProvider.GetFunction();
        return function.CalculateFee(weight.RefTime);
    }
}