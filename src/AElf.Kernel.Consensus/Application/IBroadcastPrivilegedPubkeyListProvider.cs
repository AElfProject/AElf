using System.Collections.Generic;

namespace AElf.Kernel.Consensus.Application
{
    public interface IBroadcastPrivilegedPubkeyListProvider
    {
        List<string> GetPubkeyList(BlockHeader blockHeader, string currentPubkey);
    }
}