using System.Threading.Tasks;
using AElf.Contracts.Consensus.AEDPoS;
using AElf.Kernel.Account.Application;
using AElf.Kernel.Consensus.Application;
using AElf.Kernel.SmartContract.Application;
using AElf.Types;
using Google.Protobuf.WellKnownTypes;
using Volo.Abp.DependencyInjection;

namespace AElf.Kernel.Consensus.AEDPoS.Application;

public interface IRandomNumberProvider
{
    Task<byte[]> GenerateRandomProofAsync(IChainContext chainContext);
}

internal class RandomNumberProvider : IRandomNumberProvider, ITransientDependency
{
    private readonly IAccountService _accountService;
    private readonly IConsensusReaderContextService _consensusReaderContextService;
    private readonly IContractReaderFactory<AEDPoSContractContainer.AEDPoSContractStub> _contractReaderFactory;

    public RandomNumberProvider(IAccountService accountService,
        IConsensusReaderContextService consensusReaderContextService,
        IContractReaderFactory<AEDPoSContractContainer.AEDPoSContractStub> contractReaderFactory)
    {
        _accountService = accountService;
        _consensusReaderContextService = consensusReaderContextService;
        _contractReaderFactory = contractReaderFactory;
    }

    public async Task<byte[]> GenerateRandomProofAsync(IChainContext chainContext)
    {
        var previousRandomHash = chainContext.BlockHeight == AElfConstants.GenesisBlockHeight
            ? Hash.Empty
            : await GetRandomHashAsync(chainContext, chainContext.BlockHeight);
        return await _accountService.ECVrfProveAsync(previousRandomHash.ToByteArray());
    }
    
    private async Task<Hash> GetRandomHashAsync(IChainContext chainContext, long blockHeight)
    {
        var contractReaderContext =
            await _consensusReaderContextService.GetContractReaderContextAsync(chainContext);
        return await _contractReaderFactory
            .Create(contractReaderContext).GetRandomHash.CallAsync(new Int64Value { Value = blockHeight });
    }
}
