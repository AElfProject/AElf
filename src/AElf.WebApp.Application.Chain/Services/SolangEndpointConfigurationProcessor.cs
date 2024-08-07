using System.Threading.Tasks;
using AElf.Kernel;
using AElf.Kernel.Configuration;
using AElf.Kernel.Consensus;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Volo.Abp.DependencyInjection;

namespace AElf.WebApp.Application.Chain.Services;

public class SolangEndpointConfigurationProcessor : IConfigurationProcessor, ITransientDependency
{
    private readonly ISolangEndpointProvider _solangEndpointProvider;

    public SolangEndpointConfigurationProcessor(ISolangEndpointProvider solangEndpointProvider)
    {
        _solangEndpointProvider = solangEndpointProvider;
    }

    public string ConfigurationName => ConsensusConstants.MiningTimeConfigurationName;

    public async Task ProcessConfigurationAsync(ByteString byteString, BlockIndex blockIndex)
    {
        var solangEndpoint = new StringValue();
        solangEndpoint.MergeFrom(byteString);
        await _solangEndpointProvider.SetSolangEndpointAsync(blockIndex, solangEndpoint.Value);
    }
}