using System.Collections.Generic;

namespace AElf.Kernel.TransactionPool.Application
{
    public interface ISystemTransactionMethodNameListProvider
    {
        List<string> GetSystemTransactionMethodNameList();
    }
}