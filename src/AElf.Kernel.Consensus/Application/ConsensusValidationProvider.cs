using System.Threading.Tasks;
using AElf.Kernel.Blockchain.Application;
using AElf.Kernel.TransactionPool.Application;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace AElf.Kernel.Consensus.Application
{
    public class ConsensusValidationProvider : IBlockValidationProvider
    {
        private readonly IConsensusService _consensusService;
        private readonly IBlockExtraDataService _blockExtraDataService;
        private readonly IIsPackageNormalTransactionProvider _isPackageNormalTransactionProvider;
        private readonly IBlockchainService _blockchainService;
        public ILogger<ConsensusValidationProvider> Logger { get; set; }

        public ConsensusValidationProvider(IConsensusService consensusService,
            IBlockExtraDataService blockExtraDataService,
            IIsPackageNormalTransactionProvider isPackageNormalTransactionProvider,
            IBlockchainService blockchainService)
        {
            _consensusService = consensusService;
            _blockExtraDataService = blockExtraDataService;
            _isPackageNormalTransactionProvider = isPackageNormalTransactionProvider;
            _blockchainService = blockchainService;
            Logger = NullLogger<ConsensusValidationProvider>.Instance;
        }

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

            if (_isPackageNormalTransactionProvider.IsPackage) return true;

            var chain = await _blockchainService.GetChainAsync();
            if (chain.BestChainHash == block.Header.PreviousBlockHash && block.Body.TransactionsCount > 4)
            {
                Logger.LogWarning("Cannot package normal transaction.");
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
            if (!isValid) return false;

            if (_isPackageNormalTransactionProvider.IsPackage) return true;

            var chain = await _blockchainService.GetChainAsync();
            if (chain.BestChainHash == block.Header.PreviousBlockHash && block.Body.TransactionsCount > 4)
            {
                Logger.LogWarning("Cannot package normal transaction.");
                return false;
            }

            return true;
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