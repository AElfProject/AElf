using Orleans.Runtime;
using Orleans.Runtime.Placement;

namespace AElf.Kernel.SmartContract.Grains;

public class CleanCacheStrategyFixedSiloDirector : IPlacementDirector

{
    public Task<SiloAddress> OnAddActivation(PlacementStrategy strategy, PlacementTarget target, IPlacementContext context)
    {
        var silos = context.GetCompatibleSilos(target).OrderBy(s => s).ToArray();
        var index = target.GrainIdentity.GetPrimaryKeyLong(out string keyExt) % silos.Length;
        return Task.FromResult(silos[index]);
    }
}