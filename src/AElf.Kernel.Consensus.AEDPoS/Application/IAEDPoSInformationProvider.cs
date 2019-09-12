using System.Collections.Generic;
using System.Threading.Tasks;

namespace AElf.Kernel.Consensus.AEDPoS.Application
{
    // ReSharper disable once InconsistentNaming
    public interface IAEDPoSInformationProvider
    {
        Task<IEnumerable<string>> GetCurrentMinerList(ChainContext chainContext);
    }
}