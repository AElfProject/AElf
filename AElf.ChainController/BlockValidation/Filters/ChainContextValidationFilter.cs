using System;
using System.Threading.Tasks;
using AElf.Common.Attributes;
using AElf.Cryptography.ECDSA;
using AElf.Kernel;
using AElf.Common;
using NLog;

// ReSharper disable once CheckNamespace
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

        public async Task<BlockValidationResult> ValidateBlockAsync(IBlock block, IChainContext context)
        {
            try
            {
                var index = block.Header.Index;
                var previousBlockHash = block.Header.PreviousBlockHash;
                
                var currentChainHeight = context.BlockHeight;
                var currentPreviousBlockHash = context.BlockHash;
    
                // Received a higher block.
                if (index > currentChainHeight + 1)
                {
                    return BlockValidationResult.Pending;
                }

                // Basically this block is just after genesis block.
                if (previousBlockHash == Hash.Genesis)
                {
                    return currentPreviousBlockHash.Equals(previousBlockHash)
                        ? BlockValidationResult.Success
                        : BlockValidationResult.IncorrectFirstBlock;
                }

                // The index of this block seems right.
                if (index == currentChainHeight + 1)
                {
                    return currentPreviousBlockHash.Equals(previousBlockHash)
                        ? BlockValidationResult.Success
                        : BlockValidationResult.IncorrectPreBlockHash;
                }

                // Check peer block.
                if (index <= currentChainHeight)
                {
                    var blockchain = _chainService.GetBlockChain(block.Header.ChainId);
                    var localBlock = await blockchain.GetBlockByHeightAsync(index);
                    if (localBlock == null)
                    {
                        return BlockValidationResult.FailedToGetBlockByHeight;
                    }
                    return localBlock.Header.GetHash().Equals(block.Header.GetHash())
                        ? BlockValidationResult.AlreadyExecuted
                        : BlockValidationResult.BranchedBlock;
                }
                
                _logger?.Error("Incomplete validation scheme.");
                return BlockValidationResult.UnknownReason;
            }
            catch (Exception e)
            {
                _logger?.Error(e, "Error while validating blocks.");
                return BlockValidationResult.FailedToCheckChainContextInvalidation;
            }
        }
    }
}