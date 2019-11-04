using System.Collections.Generic;

namespace AElf.Kernel.SmartContract.Application
{
    public interface ISystemTransactionMethodNameListProvider
    {
        List<string> GetSystemTransactionMethodNameList();
    }
}