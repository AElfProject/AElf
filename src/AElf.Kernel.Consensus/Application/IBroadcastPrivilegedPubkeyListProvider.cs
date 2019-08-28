using System.Collections.Generic;
using System.Threading.Tasks;

namespace AElf.Kernel.Consensus.Application
{
    public interface IBroadcastPrivilegedPubkeyListProvider
    {
        Task<List<string>> GetPubkeyList(BlockHeader blockHeader);
    }
}