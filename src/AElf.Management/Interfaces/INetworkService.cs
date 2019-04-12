using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Management.Models;

namespace AElf.Management.Interfaces
{
    public interface INetworkService
    {
        //Task<PoolStateResult> GetPoolState(string chainId);

        Task<PeerResult> GetPeers(string chainId);

        Task RecordPoolState(string chainId, DateTime time, int requestPoolSize, int receivePoolSize);

        Task<List<PoolStateHistory>> GetPoolStateHistory(string chainId);
    }
}