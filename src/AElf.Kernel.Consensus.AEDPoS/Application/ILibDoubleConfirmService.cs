using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Types;

namespace AElf.Kernel.Consensus.AEDPoS.Application
{
    public interface ILibDoubleConfirmService
    {
        Task ApplyBestChainFoundEventAsync(IEnumerable<Hash> blockHashes);
        Task ApplyIrreversibleBlockFoundEventAsync(IEnumerable<Hash> blockHashes);
    }
}