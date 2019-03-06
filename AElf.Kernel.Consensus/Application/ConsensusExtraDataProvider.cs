using System.Linq;
using System.Threading.Tasks;
using AElf.Kernel.Blockchain.Application;
using Google.Protobuf;

namespace AElf.Kernel.Consensus.Application
{
    public class ConsensusExtraDataProvider : IBlockExtraDataProvider
    {
        private readonly IConsensusService _consensusService;

        public ConsensusExtraDataProvider(IConsensusService consensusService)
        {
            _consensusService = consensusService;
        }

        public async Task<ByteString> FillExtraDataAsync(BlockHeader blockHeader)
        {
            if (blockHeader.Height == 1 || !blockHeader.BlockExtraDatas.Any())
            {
                return null;
            }

            var consensusInformation = await _consensusService.GetNewConsensusInformationAsync();

            return consensusInformation == null ? null : ByteString.CopyFrom(consensusInformation);
        }
    }
}