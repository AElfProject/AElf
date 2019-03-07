using System.Threading.Tasks;
using AElf.Kernel.Blockchain.Application;
using AElf.Kernel.Consensus.Application;

namespace AElf.Kernel.Consensus.DPoS.Application
{
    public class ConsensusValidationProvider : IBlockValidationProvider
    {
        private readonly IConsensusService _consensusService;
        private readonly IBlockExtraDataService _blockExtraDataService;

        public ConsensusValidationProvider(IConsensusService consensusService, IBlockExtraDataService blockExtraDataService)
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

            var result = await _consensusService.ValidateConsensusBeforeExecutionAsync(block.Header.PreviousBlockHash,
                block.Height - 1,
                _blockExtraDataService.GetExtraDataFromBlockHeader("Consensus", block.Header).ToByteArray());
            return result;
        }

        public async Task<bool> ValidateBlockAfterExecuteAsync(IBlock block)
        {
            if (block.Height == 1)
            {
                return true;
            }

            if (block.Header.BlockExtraDatas.Count == 0)
            {
                return true;
            }

            var result = await _consensusService.ValidateConsensusAfterExecutionAsync(block.Header.PreviousBlockHash,
                block.Height - 1,
                _blockExtraDataService.GetExtraDataFromBlockHeader("Consensus", block.Header).ToByteArray());
            return result;
        }
    }
}