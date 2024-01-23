using Orleans;

namespace AElf.Kernel.SmartContract.Orleans;

public interface ISiloClusterClientContext
{
    public IClusterClient GetClusterClient();
}