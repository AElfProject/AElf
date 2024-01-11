using Orleans.Runtime;
using Orleans.Runtime.Placement;

namespace AElf.Kernel.SmartContract.Grains;

public class CleanCacheStrategyFixedSiloDirector : IPlacementDirector

{
    public Task<SiloAddress> OnAddActivation(PlacementStrategy strategy, PlacementTarget target, IPlacementContext context)
    {
        var silos = context.GetCompatibleSilos(target).ToArray();
        var index = target.GrainIdentity.GetPrimaryKeyLong(out _) % silos.Length;
        return Task.FromResult(silos[index]);
    }
}