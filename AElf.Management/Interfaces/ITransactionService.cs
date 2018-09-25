using System;

namespace AElf.Management.Interfaces
{
    public interface ITransactionService
    {
        ulong GetPoolSize(string chainId);

        void RecordPoolSize(string chainId, DateTime time, ulong poolSize);
    }
}