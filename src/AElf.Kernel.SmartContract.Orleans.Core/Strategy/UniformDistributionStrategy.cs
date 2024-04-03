using Orleans.Placement;
using Orleans.Runtime;

namespace AElf.Kernel.SmartContract.Orleans.Strategy;

[Serializable]
public class UniformDistributionStrategy : PlacementStrategy
{

}
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
public sealed class UniformDistributionAttribute : PlacementAttribute
{
    public UniformDistributionAttribute() :
        base(new UniformDistributionStrategy())
    {
    }
}