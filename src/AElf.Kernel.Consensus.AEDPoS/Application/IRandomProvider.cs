using System.Threading.Tasks;
using AElf.Kernel.Account.Application;
using AElf.Types;
using Volo.Abp.DependencyInjection;

namespace AElf.Kernel.Consensus.AEDPoS.Application;

public interface IRandomProvider
{
    Task<byte[]> GenerateRandomProveAsync(IChainContext chainContext);
}

public class RandomProvider : IRandomProvider, ITransientDependency
{
    private readonly IAEDPoSInformationProvider _aedPoSInformationProvider;
    private readonly IAccountService _accountService;

    public RandomProvider(IAEDPoSInformationProvider aedPoSInformationProvider, IAccountService accountService)
    {
        _aedPoSInformationProvider = aedPoSInformationProvider;
        _accountService = accountService;
    }

    public async Task<byte[]> GenerateRandomProveAsync(IChainContext chainContext)
    {
        var previousRandomHash = chainContext.BlockHeight == AElfConstants.GenesisBlockHeight
            ? Hash.Empty
            : await _aedPoSInformationProvider.GetRandomHashAsync(chainContext, chainContext.BlockHeight);
        return await _accountService.ECVrfProveAsync(previousRandomHash.ToByteArray());
    }
}
