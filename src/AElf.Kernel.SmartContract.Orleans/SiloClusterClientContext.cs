using Orleans;

namespace AElf.Kernel.SmartContract.Orleans;

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