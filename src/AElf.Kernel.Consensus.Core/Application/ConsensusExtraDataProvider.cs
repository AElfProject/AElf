using System.Threading.Tasks;
using Google.Protobuf;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace AElf.Kernel.Consensus.Application;

public class ConsensusExtraDataProvider : IConsensusExtraDataProvider
{
    private readonly IConsensusService _consensusService;

    /// <summary>
    ///     Add an empty ctor for cases only need BlockHeaderExtraDataKey.
    /// </summary>
    public ConsensusExtraDataProvider()
    {
    }

    public ConsensusExtraDataProvider(IConsensusService consensusService)
    {
        _consensusService = consensusService;

        Logger = NullLogger<ConsensusExtraDataProvider>.Instance;
    }

    public ILogger<ConsensusExtraDataProvider> Logger { get; set; }
    public string BlockHeaderExtraDataKey => ConsensusConstants.ConsensusExtraDataKey;

    public async Task<ByteString> GetBlockHeaderExtraDataAsync(BlockHeader blockHeader)
    {
        if (blockHeader.Height == AElfConstants.GenesisBlockHeight) return null;

        var consensusInformation = await _consensusService.GetConsensusExtraDataAsync(new ChainContext
        {
            BlockHash = blockHeader.PreviousBlockHash,
            BlockHeight = blockHeader.Height - 1
        });

        return consensusInformation == null ? ByteString.Empty : ByteString.CopyFrom(consensusInformation);
    }
}