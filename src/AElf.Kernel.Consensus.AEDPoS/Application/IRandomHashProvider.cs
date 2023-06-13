using System.Threading.Tasks;
using AElf.Kernel.Account.Application;
using AElf.Types;

namespace AElf.Kernel.Consensus.AEDPoS.Application;

public interface IRandomHashProvider
{
    Task<Hash> GenerateRandomHashAsync(IChainContext chainContext);
}

public class RandomHashProvider : IRandomHashProvider
{
    private readonly IAEDPoSInformationProvider _aedPoSInformationProvider;
    private readonly IAccountService _accountService;

    public RandomHashProvider(IAEDPoSInformationProvider aedPoSInformationProvider, IAccountService accountService)
    {
        _aedPoSInformationProvider = aedPoSInformationProvider;
        _accountService = accountService;
    }

    public async Task<Hash> GenerateRandomHashAsync(IChainContext chainContext)
    {
        var previousRandomHash =
            await _aedPoSInformationProvider.GetRandomHashAsync(chainContext, chainContext.BlockHeight);
        // TODO: Use real random hash.
        var randomHash = Hash.Empty;

        return randomHash;
    }
}
