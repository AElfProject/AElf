using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Kernel.Consensus.Application;

namespace AElf.Kernel.Consensus.PoA.Application;

public class PoABroadcastPrivilegedPubkeyListProvider : IBroadcastPrivilegedPubkeyListProvider
{
    public async Task<List<string>> GetPubkeyList(BlockHeader blockHeader)
    {
        return new List<string>();
    }
}