using System.Collections.Generic;

namespace AElf.Kernel.Consensus.Application
{
    public interface IConsensusCoreTransactionMethodNameListProvider
    {
        List<string> GetCoreTransactionMethodNameList();
    }
}