using System;
using System.Linq;
using System.Threading.Tasks;
using AElf.Kernel.Blockchain.Application;
using AElf.Kernel.Miner.Application;
using AElf.Kernel.TransactionPool.Application;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace AElf.Kernel.Consensus.Application
{
    public class ConsensusValidationProvider : IBlockValidationProvider
    {
        private readonly IConsensusService _consensusService;
        private readonly ITransactionInclusivenessProvider _transactionInclusivenessProvider;
        private readonly IBlockchainService _blockchainService;
        private readonly IConsensusExtraDataExtractor _consensusExtraDataExtractor;
        private readonly int _systemTransactionCount;
        public ILogger<ConsensusValidationProvider> Logger { get; set; }

        public ConsensusValidationProvider(IServiceProvider serviceProvider)
        {
            _consensusService = serviceProvider.GetService<IConsensusService>();
            _transactionInclusivenessProvider = serviceProvider.GetService<ITransactionInclusivenessProvider>();
            _blockchainService = serviceProvider.GetService<IBlockchainService>();
            _consensusExtraDataExtractor = serviceProvider.GetService<IConsensusExtraDataExtractor>();
            _systemTransactionCount = serviceProvider.GetServices<ISystemTransactionGenerator>().Count();
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

            var consensusExtraData = _consensusExtraDataExtractor.ExtractConsensusExtraData(block.Header);
            if (consensusExtraData == null || consensusExtraData.IsEmpty)
            {
                Logger.LogWarning($"Invalid consensus extra data {block}");
                return false;
            }

            return await ValidateTransactionCount(block);
        }

        public async Task<bool> ValidateBlockBeforeExecuteAsync(IBlock block)
        {
            if (block.Header.Height == Constants.GenesisBlockHeight)
                return true;

            var consensusExtraData = _consensusExtraDataExtractor.ExtractConsensusExtraData(block.Header);
            if (consensusExtraData == null || consensusExtraData.IsEmpty)
            {
                Logger.LogWarning($"Invalid consensus extra data {block}");
                return false;
            }

            var isValid = await _consensusService.ValidateConsensusBeforeExecutionAsync(new ChainContext
            {
                BlockHash = block.Header.PreviousBlockHash,
                BlockHeight = block.Header.Height - 1
            }, consensusExtraData.ToByteArray());
            if (!isValid) return false;

            return await ValidateTransactionCount(block);
        }

        private async Task<bool> ValidateTransactionCount(IBlock block)
        {
            if (_transactionInclusivenessProvider.IsTransactionPackable) return true;

            var chain = await _blockchainService.GetChainAsync();
            if (chain.BestChainHash == block.Header.PreviousBlockHash &&
                block.Body.TransactionsCount > _systemTransactionCount)
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

            var consensusExtraData = _consensusExtraDataExtractor.ExtractConsensusExtraData(block.Header);
            if (consensusExtraData == null || consensusExtraData.IsEmpty)
            {
                Logger.LogWarning($"Invalid consensus extra data {block}");
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