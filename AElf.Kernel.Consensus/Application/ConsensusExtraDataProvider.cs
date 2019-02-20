using System.Threading.Tasks;
using AElf.Common;
using AElf.Kernel.Services;
using AElf.Kernel.Blockchain.Application;
using Google.Protobuf;
using Microsoft.Extensions.Options;
namespace AElf.Kernel.Consensus.Application
{
    public class ConsensusExtraDataProvider : IBlockExtraDataProvider
    {
        private readonly IConsensusService _consensusService;

        public ConsensusExtraDataProvider(IConsensusService consensusService)
        {
            _consensusService = consensusService;
        }
        
        public async Task FillExtraData(int chainId, Block block)
        {
            if (block.Header.BlockExtraData == null)
            {
                block.Header.BlockExtraData = new BlockExtraData();
            }

            var consensusInformation =
                await _consensusService.GetNewConsensusInformationAsync(chainId);

            block.Header.BlockExtraData.ConsensusInformation = ByteString.CopyFrom(consensusInformation);
        }

        public async Task<bool> ValidateExtraData(int chainId, Block block)
        {
            var consensusInformation = block.Header.BlockExtraData.ConsensusInformation;

            return await _consensusService.ValidateConsensusAsync(chainId, block.GetHash(), block.Height,
                consensusInformation.ToByteArray());
        }
    }
}