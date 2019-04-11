using System.Threading.Tasks;
using AElf.Consensus.DPoS;

namespace AElf.Kernel.Consensus.DPoS
{
    public interface IDPoSInformationProvider
    {
        Task<Miners> GetCurrentMiners(ChainContext chainContext);
    }
}