using System.Linq;
using System.Threading.Tasks;
using AElf.Kernel.Blockchain.Application;
using AElf.Kernel.Consensus.Application;
using Google.Protobuf;

namespace AElf.Kernel.Consensus.DPoS.Application
{
    // ReSharper disable once InconsistentNaming
    public class ConsensusExtraDataProvider : IBlockValidationProvider, IBlockExtraDataProvider
    {
        private readonly IConsensusService _consensusService;
        private readonly IBlockExtraDataService _blockExtraDataService;

        public ConsensusExtraDataProvider(IConsensusService consensusService, IBlockExtraDataService blockExtraDataService)
        {
            _consensusService = consensusService;
            _blockExtraDataService = blockExtraDataService;
        }

        public async Task<bool> ValidateBlockBeforeExecuteAsync(IBlock block)
        {
            if (block.Height == 1)
            {
                return true;
            }

            if (block.Header.BlockExtraDatas.Count == 0)
            {
                return true;
            }

            var result = await _consensusService.ValidateConsensusAsync(block.Header.PreviousBlockHash,
                block.Height - 1,
                _blockExtraDataService.GetBlockExtraData(GetType(), block.Header).ToByteArray());
            return result;
        }

        public Task<bool> ValidateBlockAfterExecuteAsync(IBlock block)
        {
            // TODO: Need a new contract method to validate consensus information after block execution.
            return Task.FromResult(true);
        }

        public async Task<ByteString> GetExtraDataAsync(BlockHeader blockHeader)
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