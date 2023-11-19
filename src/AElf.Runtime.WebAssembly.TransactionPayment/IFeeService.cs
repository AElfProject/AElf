using Volo.Abp.DependencyInjection;

namespace AElf.Runtime.WebAssembly.TransactionPayment;

public interface IFeeService
{
    long CalculateFees(Weight? weight);
}

public class FeeService : IFeeService, ITransientDependency
{
    private readonly IEnumerable<IFeeProvider> _feeProviders;

    public FeeService(IEnumerable<IFeeProvider> feeProviders)
    {
        _feeProviders = feeProviders;
    }

    public long CalculateFees(Weight? weight)
    {
        return weight == null
            ? 0
            : _feeProviders.Select(feeFunctionProvider => feeFunctionProvider.GetWeightFee(weight)).Sum();
    }
}