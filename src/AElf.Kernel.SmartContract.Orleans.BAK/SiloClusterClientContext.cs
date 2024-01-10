using Orleans;

namespace AElf.Kernel.SmartContract.Grain;

public class SiloClusterClientContext : ISiloClusterClientContext
{
    private readonly IClusterClient _clusterClient;

    public SiloClusterClientContext(IClusterClient clusterClient)
    {
        _clusterClient = clusterClient;
    }

    public IClusterClient GetClusterClient()
    {
        return _clusterClient;
    }
}