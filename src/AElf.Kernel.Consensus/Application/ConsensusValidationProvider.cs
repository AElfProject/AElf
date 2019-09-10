using System.Threading.Tasks;
using AElf.Kernel.Blockchain.Application;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace AElf.Kernel.Consensus.Application
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
            Logger = NullLogger<ConsensusValidationProvider>.Instance;
        }

        #pragma warning disable 1998
        public async Task<bool> ValidateBeforeAttachAsync(IBlock block)
        {
            if (block.Header.Height == Constants.GenesisBlockHeight)
                return true;

            if (block.Header.ExtraData.Count == 0)
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
            if (block.Header.Height == Constants.GenesisBlockHeight)
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
            if (block.Header.Height == Constants.GenesisBlockHeight)
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