using Orleans.Placement;
using Orleans.Runtime;

namespace AElf.Kernel.SmartContract.Grains;

[Serializable]
public class CleanCacheStrategy :  PlacementStrategy
{

}
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
public sealed class CleanCacheAttribute : PlacementAttribute
{
    public CleanCacheAttribute() :
        base(new CleanCacheStrategy())
    {
    }
}