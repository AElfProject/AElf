using System.Threading.Tasks;
using AElf.Kernel;
using AElf.Kernel.Account;
using AElf.Kernel.BlockService;
using Google.Protobuf;

namespace AElf.Consensus
{
    public class ConsensusExtraDataProvider : IBlockExtraDataProvider
    {
        private readonly IConsensusService _consensusService;

        public ConsensusExtraDataProvider(IConsensusService consensusService)
        {
            _consensusService = consensusService;
        }
        
        public async Task FillExtraData(Block block)
        {
            if (block.Header.BlockExtraData == null)
            {
                block.Header.BlockExtraData = new BlockExtraData();
            }

            var consensusInformation = await _consensusService.GetNewConsensusInformation(block.Header.ChainId);

            block.Header.BlockExtraData.ConsensusInformation = ByteString.CopyFrom(consensusInformation);
        }

        public async Task<bool> ValidateExtraData(Block block)
        {
            var consensusInformation = block.Header.BlockExtraData.ConsensusInformation;

            return await _consensusService.ValidateConsensus(block.Header.ChainId, consensusInformation.ToByteArray());
        }
    }
}