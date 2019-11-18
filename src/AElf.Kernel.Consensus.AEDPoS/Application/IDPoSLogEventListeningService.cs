using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Types;

namespace AElf.Kernel.Consensus.AEDPoS.Application
{
    public interface IDPoSLogEventListeningService
    {
        Task ApplyAsync(IEnumerable<Hash> blockHashes);
    }
}