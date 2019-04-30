using System.Linq;
using System.Threading.Tasks;
using AElf.Kernel.Blockchain.Application;
using AElf.Kernel.Consensus.Application;
using Google.Protobuf;

namespace AElf.Kernel.Consensus.DPoS.Application
{
    // ReSharper disable once InconsistentNaming
    public class ConsensusExtraDataProvider : IBlockExtraDataProvider
    {
        private readonly IConsensusService _consensusService;

        public ConsensusExtraDataProvider(IConsensusService consensusService)
        {
            _consensusService = consensusService;
        }

        public async Task<ByteString> GetExtraDataForFillingBlockHeaderAsync(BlockHeader blockHeader)
        {
            if (blockHeader.Height == 1 || blockHeader.BlockExtraDatas.Any())
            {
                return null;
            }

            var consensusInformation = await _consensusService.GetInformationToUpdateConsensusAsync(new ChainContext
            {
                BlockHash = blockHeader.PreviousBlockHash, 
                BlockHeight = blockHeader.Height - 1
            });

            return consensusInformation == null ? ByteString.Empty : ByteString.CopyFrom(consensusInformation);
        }
    }
}