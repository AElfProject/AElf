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
        
        public async Task FillExtraDataAsync(int chainId, Block block)
        {
            if (block.Header.BlockExtraData == null)
            {
                block.Header.BlockExtraData = new BlockExtraData();
            }

            var consensusInformation =
                await _consensusService.GetNewConsensusInformationAsync(chainId);

            block.Header.BlockExtraData.ConsensusInformation = ByteString.CopyFrom(consensusInformation);
        }

        public async Task<bool> ValidateExtraDataAsync(int chainId, Block block)
        {
            var consensusInformation = block.Header.BlockExtraData.ConsensusInformation;

            return await _consensusService.ValidateConsensusAsync(chainId, block.GetHash(), block.Height,
                consensusInformation.ToByteArray());
        }
    }
}