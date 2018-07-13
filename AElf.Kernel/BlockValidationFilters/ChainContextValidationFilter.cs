using System;
using System.Threading.Tasks;
using AElf.Common.Attributes;
using AElf.Cryptography.ECDSA;
using AElf.Kernel.Managers;
using AElf.Kernel.Services;
using NLog;
using ServiceStack;

namespace AElf.Kernel.BlockValidationFilters
{
    [LoggerName(nameof(ChainContextValidationFilter))]
    public class ChainContextValidationFilter : IBlockValidationFilter
    {
        private readonly IBlockManager _blockManager;
        private readonly ILogger _logger;

        public ChainContextValidationFilter(IBlockManager blockManager, ILogger logger)
        {
            _blockManager = blockManager;
            _logger = logger;
        }

        public async Task<ValidationError> ValidateBlockAsync(IBlock block, IChainContext context, ECKeyPair keyPair)
        {
            try
            {
                /*
                    1' block height
                    2' previous block hash
                */
    
                var index = block.Header.Index;
                var previousBlockHash = block.Header.PreviousBlockHash;
    
                // return success if genesis block
                /*if (index == 0 && previousBlockHash.Equals(Hash.Zero))
                    return TxInsertionAndBroadcastingError.Valid;*/
    
                var currentChainHeight = context.BlockHeight;
                var currentPreviousBlockHash = context.BlockHash;
    
                // other block needed before this one
                if (index > currentChainHeight)
                {
                    _logger?.Trace("Received block index:" + index);
                    _logger?.Trace("Current chain height:" + currentChainHeight);
                    
                    return ValidationError.Pending;
                }
                
                // can be added to chain
                if (currentChainHeight == index)
                {
                    if (!currentPreviousBlockHash.Equals(previousBlockHash))
                    {
                        _logger?.Trace("context.BlockHash:" + currentPreviousBlockHash.ToHex());
                        _logger?.Trace("block.Header.PreviousBlockHash:" + previousBlockHash.ToHex());
                    }
                    
                    return currentPreviousBlockHash.Equals(previousBlockHash)
                        ? ValidationError.Success
                        : ValidationError.OrphanBlock;
                }
                
                if (index < currentChainHeight)
                {
                    var b = await _blockManager.GetBlockByHeight(block.Header.ChainId, index);
                    if (b == null)
                    {
                        return ValidationError.FailedToGetBlockByHeight;
                    }
                    return b.Header.GetHash().Equals(block.Header.GetHash())
                        ? ValidationError.AlreadyExecuted
                        : ValidationError.OrphanBlock;
                }
                
                return ValidationError.DontKnowReason;
            }
            catch (Exception e)
            {
                _logger?.Error(e, "Error while validating blocks.");
                return ValidationError.FailedToCheckChainContextInvalidation;
            }
        }
    }
}