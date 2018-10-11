using System;
using System.Threading.Tasks;
using AElf.ChainController;
using AElf.Common.Attributes;
using AElf.Cryptography.ECDSA;
using AElf.Kernel;
using AElf.Common;
using NLog;

namespace AElf.ChainController
{
    [LoggerName(nameof(ChainContextValidationFilter))]
    public class ChainContextValidationFilter : IBlockValidationFilter
    {
        private readonly IChainService _chainService;
        private readonly ILogger _logger;

        public ChainContextValidationFilter(IChainService chainService, ILogger logger)
        {
            _chainService = chainService;
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
                if (index > currentChainHeight + 1)
                {
                    _logger?.Trace("Received block index:" + index);
                    _logger?.Trace("Current chain height:" + currentChainHeight);
                    
                    return ValidationError.Pending;
                }
                
                // can be added to chain
                if (previousBlockHash == Hash.Genesis ||  index == currentChainHeight + 1)
                {
                    if (currentPreviousBlockHash.Equals(previousBlockHash))
                        return ValidationError.Success;

                    _logger?.Trace("context.BlockHash:" + currentPreviousBlockHash.DumpHex());
                    _logger?.Trace("block.Header.PreviousBlockHash:" + previousBlockHash.DumpHex());

                    return ValidationError.IncorrectPreBlockHash;
                }
                
                if (index <= currentChainHeight)
                {
                    var blockchain = _chainService.GetBlockChain(block.Header.ChainId);
                    var b = await blockchain.GetBlockByHeightAsync(index);
                    if (b == null)
                    {
                        return ValidationError.FailedToGetBlockByHeight;
                    }
                    return b.Header.GetHash().Equals(block.Header.GetHash())
                        ? ValidationError.AlreadyExecuted
                        : ValidationError.OrphanBlock;
                }
                
                _logger?.Error("Incomplete validation scheme.");
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