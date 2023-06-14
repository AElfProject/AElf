using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Kernel;
using AElf.Kernel.Consensus.AEDPoS.Application;
using AElf.Types;

namespace AElf.Contracts.Economic.TestBase;

public class MockAEDPoSInformationProvider : IAEDPoSInformationProvider
{
    public Task<IEnumerable<string>> GetCurrentMinerListAsync(ChainContext chainContext)
    {
        throw new System.NotImplementedException();
    }

    public async Task<Hash> GetRandomHashAsync(IChainContext chainContext, long blockHeight)
    {
        return Hash.Empty;
    }
}