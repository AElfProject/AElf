using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Types;

namespace AElf.Kernel.Consensus.AEDPoS.Application;

// ReSharper disable once InconsistentNaming
public interface IAEDPoSInformationProvider
{
    Task<IEnumerable<string>> GetCurrentMinerListAsync(ChainContext chainContext);
}