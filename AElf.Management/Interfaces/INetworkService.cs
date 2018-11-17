using System;
using System.Collections.Generic;
using AElf.Management.Models;

namespace AElf.Management.Interfaces
{
    public interface INetworkService
    {
        PoolStateResult GetPoolState(string chainId);

        PeerResult GetPeers(string chainId);

        void RecordPoolState(string chainId, DateTime time, int requestPoolSize, int receivePoolSize);

        List<PoolStateHistory> GetPoolStateHistory(string chainId);
    }
}