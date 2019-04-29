using System.Collections.Generic;
using System.Threading.Tasks;

namespace AElf.Kernel.Consensus.DPoS
{
    public interface IDPoSInformationProvider
    {
        Task<IEnumerable<string>> GetCurrentMiners(ChainContext chainContext);
    }
}