using System.Threading.Tasks;
using AElf.Kernel.Configuration;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Volo.Abp.DependencyInjection;

namespace AElf.Kernel.Consensus.Application;

public class MiningTimeConfigurationProcessor : IConfigurationProcessor, ITransientDependency
{
    private readonly IMiningTimeProvider _miningTimeProvider;

    public MiningTimeConfigurationProcessor(IMiningTimeProvider miningTimeProvider)
    {
        _miningTimeProvider = miningTimeProvider;
    }

    public string ConfigurationName => ConsensusConstants.MiningTimeConfigurationName;

    public async Task ProcessConfigurationAsync(ByteString byteString, BlockIndex blockIndex)
    {
        var limit = new Int64Value();
        limit.MergeFrom(byteString);
        if (limit.Value < 0) return;
        await _miningTimeProvider.SetLimitMillisecondsOfMiningBlockAsync(blockIndex, limit.Value);
    }
}