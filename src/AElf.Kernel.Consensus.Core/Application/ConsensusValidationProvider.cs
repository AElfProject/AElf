using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Kernel.Blockchain.Application;
using AElf.Kernel.Miner.Application;
using AElf.Kernel.Txn.Application;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace AElf.Kernel.Consensus.Application
{
    public class ConsensusValidationProvider : IBlockValidationProvider
    {
        private readonly IConsensusService _consensusService;
        private readonly ITransactionPackingOptionProvider _transactionPackingOptionProvider;
        private readonly IBlockchainService _blockchainService;
        private readonly IConsensusExtraDataExtractor _consensusExtraDataExtractor;
        private readonly int _systemTransactionCount;
        public ILogger<ConsensusValidationProvider> Logger { get; set; }

        public ConsensusValidationProvider(IConsensusService consensusService,
            ITransactionPackingOptionProvider transactionPackingOptionProvider,
            IBlockchainService blockchainService,
            IConsensusExtraDataExtractor consensusExtraDataExtractor,
            IEnumerable<ISystemTransactionGenerator> systemTransactionGenerators)
        {
            _consensusService = consensusService;
            _transactionPackingOptionProvider = transactionPackingOptionProvider;
            _blockchainService = blockchainService;
            _consensusExtraDataExtractor = consensusExtraDataExtractor;
            _systemTransactionCount = systemTransactionGenerators.Count();

            Logger = NullLogger<ConsensusValidationProvider>.Instance;
        }

        public async Task<bool> ValidateBeforeAttachAsync(IBlock block)
        {
            if (block.Header.Height == AElfConstants.GenesisBlockHeight)
                return true;

            if (block.Header.ExtraData.Count == 0)
            {
                Logger.LogDebug($"Block header extra data is empty {block}");
                return false;
            }

            var consensusExtraData = _consensusExtraDataExtractor.ExtractConsensusExtraData(block.Header);
            if (consensusExtraData == null || consensusExtraData.IsEmpty)
            {
                Logger.LogDebug($"Invalid consensus extra data {block}");
                return false;
            }

            return true;
        }

        public async Task<bool> ValidateBlockBeforeExecuteAsync(IBlock block)
        {
            if (block.Header.Height == AElfConstants.GenesisBlockHeight)
                return true;

            var consensusExtraData = _consensusExtraDataExtractor.ExtractConsensusExtraData(block.Header);
            if (consensusExtraData == null || consensusExtraData.IsEmpty)
            {
                Logger.LogDebug($"Invalid consensus extra data {block}");
                return false;
            }

            var isValid = await _consensusService.ValidateConsensusBeforeExecutionAsync(new ChainContext
            {
                BlockHash = block.Header.PreviousBlockHash,
                BlockHeight = block.Header.Height - 1
            }, consensusExtraData.ToByteArray());
            if (!isValid) return false;

            return ValidateTransactionCount(block);
        }

        private bool ValidateTransactionCount(IBlock block)
        {
            var chainContext = new ChainContext
            {
                BlockHash = block.Header.PreviousBlockHash,
                BlockHeight = block.Header.Height - 1
            };
            if (_transactionPackingOptionProvider.IsTransactionPackable(chainContext))
                return true;

            if (block.Body.TransactionsCount > _systemTransactionCount)
            {
                Logger.LogDebug("Cannot package normal transaction.");
                return false;
            }

            return true;
        }

        public async Task<bool> ValidateBlockAfterExecuteAsync(IBlock block)
        {
            if (block.Header.Height == AElfConstants.GenesisBlockHeight)
                return true;

            var consensusExtraData = _consensusExtraDataExtractor.ExtractConsensusExtraData(block.Header);
            if (consensusExtraData == null || consensusExtraData.IsEmpty)
            {
                Logger.LogDebug($"Invalid consensus extra data {block}");
                return false;
            }

            var isValid = await _consensusService.ValidateConsensusAfterExecutionAsync(new ChainContext
            {
                BlockHash = block.GetHash(),
                BlockHeight = block.Header.Height
            }, consensusExtraData.ToByteArray());

            return isValid;
        }
    }
}