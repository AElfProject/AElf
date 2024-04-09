using System.Threading.Tasks;
using AElf.CSharp.Core.Extension;
using AElf.Kernel.Blockchain.Application;
using AElf.Kernel.Configuration;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Volo.Abp.DependencyInjection;

namespace AElf.Kernel.Consensus.Application;

public interface IMiningTimeProvider
{
    Task<long> GetLimitMillisecondsOfMiningBlockAsync();
}

public class MiningTimeProvider : IMiningTimeProvider, ISingletonDependency
{
    private readonly IConfigurationService _configurationService;
    private readonly IBlockchainService _blockchainService;

    public MiningTimeProvider(IBlockchainService blockchainService, IConfigurationService configurationService)
    {
        _blockchainService = blockchainService;
        _configurationService = configurationService;
    }

    public async Task<long> GetLimitMillisecondsOfMiningBlockAsync()
    {
        var chain = await _blockchainService.GetChainAsync();
        var miningTimeBytes = await _configurationService.GetConfigurationDataAsync(
            ConsensusConstants.MiningTimeConfigurationName,
            new ChainContext
            {
                BlockHeight = chain.BestChainHeight,
                BlockHash = chain.BestChainHash
            });
        var miningTime = new Int64Value();
        miningTime.MergeFrom(miningTimeBytes);
        return miningTime.Value;
    }
}