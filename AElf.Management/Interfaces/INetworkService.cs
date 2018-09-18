using AElf.Management.Models;

namespace AElf.Management.Interfaces
{
    public interface INetworkService
    {
        PoolStateResult GetPoolState(string chainId);

        PeerResult GetPeers(string chainId);
    }
}