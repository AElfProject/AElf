using System;
using System.Collections.Generic;
using AElf.Management.Models;

namespace AElf.Management.Interfaces
{
    public interface INodeService
    {
        bool IsAlive(string chainId);

        bool IsForked(string chainId);

        void RecordPoolState(string chainId, DateTime time, bool isAlive, bool isForked);

        List<NodeStateHistory> GetHistoryState(string chainId);

        void RecordBlockInfo(string chainId);
    }
}