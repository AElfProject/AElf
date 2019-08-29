using System.Collections.Generic;

namespace AElf.Kernel.Consensus.Application
{
    public interface ISystemTransactionMethodNameListProvider
    {
        List<string> GetSystemTransactionMethodNameList();
    }
}