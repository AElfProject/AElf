using System.Threading.Tasks;
using AElf.Kernel.Blockchain.Application;
using AElf.Kernel.Consensus.Application;

namespace AElf.Kernel.Consensus.AElfConsensus.Application
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

        public async Task<bool> ValidateBeforeAttachAsync(IBlock block)
        {
            if (block.Height == KernelConstants.GenesisBlockHeight)
                return true;

            if (block.Header.BlockExtraDatas.Count == 0)
                return false;

            var consensusExtraData = _blockExtraDataService.GetExtraDataFromBlockHeader("Consensus", block.Header);
            if (consensusExtraData == null || consensusExtraData.IsEmpty)
                return false;

            return true;
        }

        public async Task<bool> ValidateBlockBeforeExecuteAsync(IBlock block)
        {
            if (block.Height == KernelConstants.GenesisBlockHeight)
                return true;

            var consensusExtraData = _blockExtraDataService.GetExtraDataFromBlockHeader("Consensus", block.Header);
            if (consensusExtraData == null || consensusExtraData.IsEmpty)
                return false;

            var isValid = await _consensusService.ValidateConsensusBeforeExecutionAsync(new ChainContext
            {
                BlockHash = block.Header.PreviousBlockHash,
                BlockHeight = block.Height - 1
            }, consensusExtraData.ToByteArray());

            return isValid;
        }

        public async Task<bool> ValidateBlockAfterExecuteAsync(IBlock block)
        {
            if (block.Height == KernelConstants.GenesisBlockHeight)
                return true;

            var consensusExtraData = _blockExtraDataService.GetExtraDataFromBlockHeader("Consensus", block.Header);
            if (consensusExtraData == null || consensusExtraData.IsEmpty)
                return false;
            var isValid = await _consensusService.ValidateConsensusAfterExecutionAsync(new ChainContext
            {
                BlockHash = block.GetHash(),
                BlockHeight = block.Height
            }, consensusExtraData.ToByteArray());

            return isValid;
        }
    }
}