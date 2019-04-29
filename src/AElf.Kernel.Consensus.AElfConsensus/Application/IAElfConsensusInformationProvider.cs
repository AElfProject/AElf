using System.Collections.Generic;
using System.Threading.Tasks;

namespace AElf.Kernel.Consensus.AElfConsensus.Application
{
    public interface IAElfConsensusInformationProvider
    {
        Task<IEnumerable<string>> GetCurrentMiners(ChainContext chainContext);
    }
}