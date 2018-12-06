using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Management.Models;

namespace AElf.Management.Interfaces
{
    public interface INodeService
    {
        Task<bool> IsAlive(string chainId);

        Task<bool> IsForked(string chainId);

        Task RecordPoolState(string chainId, DateTime time);

        Task<List<NodeStateHistory>> GetHistoryState(string chainId);

        Task RecordBlockInfo(string chainId);
    }
}