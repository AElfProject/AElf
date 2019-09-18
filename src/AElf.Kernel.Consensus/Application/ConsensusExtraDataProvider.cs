using System.Linq;
using System.Threading.Tasks;
using AElf.Kernel.Blockchain.Application;
using Google.Protobuf;
using Microsoft.Extensions.Logging;

namespace AElf.Kernel.Consensus.Application
{
    // ReSharper disable once InconsistentNaming
    public class ConsensusExtraDataProvider : IBlockExtraDataProvider
    {
        private readonly IConsensusService _consensusService;
        public ILogger<ConsensusExtraDataProvider> Logger { get; set; }

        public ConsensusExtraDataProvider(IConsensusService consensusService)
        {
            _consensusService = consensusService;
        }

        public async Task<ByteString> GetExtraDataForFillingBlockHeaderAsync(BlockHeader blockHeader)
        {
            if (blockHeader.Height == 1 || blockHeader.ExtraData.Any())
            {
                return null;
            }

            var consensusInformation = await _consensusService.GetInformationToUpdateConsensusAsync(new ChainContext
            {
                BlockHash = blockHeader.PreviousBlockHash, 
                BlockHeight = blockHeader.Height - 1
            });

            Logger.LogTrace("Consensus extra data generated.");
            return consensusInformation == null ? ByteString.Empty : ByteString.CopyFrom(consensusInformation);
        }
    }
}