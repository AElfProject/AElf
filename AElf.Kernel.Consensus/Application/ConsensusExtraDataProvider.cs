using System.Threading.Tasks;
using AElf.Common;
using AElf.Kernel.Blockchain.Application;
using Google.Protobuf;
using Microsoft.Extensions.Options;

namespace AElf.Kernel.Consensus.Application
{
    public class ConsensusExtraDataProvider : IBlockExtraDataProvider
    {
        private readonly IConsensusService _consensusService;

        private readonly ChainOptions _chainOptions;

        public ConsensusExtraDataProvider(IOptions<ChainOptions> options,IConsensusService consensusService)
        {
            _consensusService = consensusService;

            _chainOptions = options.Value;
        }
        
        public async Task FillExtraDataAsync(Block block)
        {
            if (block.Header.BlockExtraData == null)
            {
                block.Header.BlockExtraData = new BlockExtraData();
            }

            var consensusInformation =
                await _consensusService.GetNewConsensusInformationAsync(_chainOptions.ChainId.ConvertBase58ToChainId());

            block.Header.BlockExtraData.ConsensusInformation = ByteString.CopyFrom(consensusInformation);
        }

        public async Task<bool> ValidateExtraDataAsync(Block block)
        {
            var consensusInformation = block.Header.BlockExtraData.ConsensusInformation;

            return await _consensusService.ValidateConsensusAsync(block.Header.ChainId, block.GetHash(), block.Height,
                consensusInformation.ToByteArray());
        }
    }
}