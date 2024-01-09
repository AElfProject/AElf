using Orleans;

namespace AElf.Kernel.SmartContract.Grain;

public interface ISiloClusterClientContext
{
    public IClusterClient GetClusterClient();
}