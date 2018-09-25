using System;

namespace AElf.Management.Interfaces
{
    public interface INodeService
    {
        bool IsAlive(string chainId);

        bool IsForked(string chainId);

        void RecordPoolState(string chainId, DateTime time, bool isAlive, bool isForked);
    }
}