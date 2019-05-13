using System.Threading.Tasks;
using AElf.Kernel.Blockchain.Application;
using AElf.Kernel.Consensus.Application;
using Microsoft.Extensions.Logging;

namespace AElf.Kernel.Consensus.DPoS.Application
{
    public class ConsensusValidationProvider : IBlockValidationProvider
    {
        private readonly IConsensusService _consensusService;
        private readonly IBlockExtraDataService _blockExtraDataService;
        public ILogger<ConsensusValidationProvider> Logger { get; set; }

        public ConsensusValidationProvider(IConsensusService consensusService, IBlockExtraDataService blockExtraDataService)
        {
            _consensusService = consensusService;
            _blockExtraDataService = blockExtraDataService;
        }

        public async Task<bool> ValidateBeforeAttachAsync(IBlock block)
        {
            if (block.Header.Height == KernelConstants.GenesisBlockHeight)
                return true;

            if (block.Header.BlockExtraDatas.Count == 0)
            {
                Logger.LogWarning($"Block header extra data is empty {block}");
                return false;
            }

            var consensusExtraData = _blockExtraDataService.GetExtraDataFromBlockHeader("Consensus", block.Header);
            if (consensusExtraData == null || consensusExtraData.IsEmpty)
            {
                Logger.LogWarning($"Consensus extra data is empty {block}");
                return false;
            }

            return true;
        }

        public async Task<bool> ValidateBlockBeforeExecuteAsync(IBlock block)
        {
            if (block.Header.Height == KernelConstants.GenesisBlockHeight)
                return true;

            var consensusExtraData = _blockExtraDataService.GetExtraDataFromBlockHeader("Consensus", block.Header);
            if (consensusExtraData == null || consensusExtraData.IsEmpty)
            {
                Logger.LogWarning($"Consensus extra data is empty {block}");
                return false;
            }

            var isValid = await _consensusService.ValidateConsensusBeforeExecutionAsync(new ChainContext
            {
                BlockHash = block.Header.PreviousBlockHash,
                BlockHeight = block.Header.Height - 1
            }, consensusExtraData.ToByteArray());

            return isValid;
        }

        public async Task<bool> ValidateBlockAfterExecuteAsync(IBlock block)
        {
            if (block.Header.Height == KernelConstants.GenesisBlockHeight)
                return true;

            var consensusExtraData = _blockExtraDataService.GetExtraDataFromBlockHeader("Consensus", block.Header);
            if (consensusExtraData == null || consensusExtraData.IsEmpty)
                return false;
            var isValid = await _consensusService.ValidateConsensusAfterExecutionAsync(new ChainContext
            {
                BlockHash = block.GetHash(),
                BlockHeight = block.Header.Height
            }, consensusExtraData.ToByteArray());

            return isValid;
        }
    }
}