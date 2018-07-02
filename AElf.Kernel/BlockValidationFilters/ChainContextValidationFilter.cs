﻿using System;
using System.Threading.Tasks;
using AElf.Cryptography.ECDSA;
using AElf.Kernel.Managers;
using AElf.Kernel.Services;
using AElf.Kernel.Types;

namespace AElf.Kernel.BlockValidationFilters
{
    public class ChainContextValidationFilter : IBlockValidationFilter
    {
        private readonly IBlockManager _blockManager;

        public ChainContextValidationFilter(IBlockManager blockManager)
        {
            _blockManager = blockManager;
        }

        public async Task<ValidationError> ValidateBlockAsync(IBlock block, IChainContext context, ECKeyPair keyPair)
        {
            /*
                1' block height
                2' previous block hash
            */

            var index = block.Header.Index;
            var previousBlockHash = block.Header.PreviousBlockHash;

            // return success if genesis block
            /*if (index == 0 && previousBlockHash.Equals(Hash.Zero))
                return ValidationError.Success;*/

            var currentChainHeight = context.BlockHeight;
            var currentPreviousBlockHash = context.BlockHash;

            // other block needed before this one
            if (index > currentChainHeight)
                return ValidationError.Pending;
            
            // can be added to chain
            if (currentChainHeight == index)
            {
                return currentPreviousBlockHash.Equals(previousBlockHash)
                    ? ValidationError.Success
                    : ValidationError.OrphanBlock;
            }
            if (index < currentChainHeight)
            {
                var b = await _blockManager.GetBlockByHeight(block.Header.ChainId, index);
                return b.Header.GetHash().Equals(block.Header.GetHash())
                    ? ValidationError.AlreadyExecuted
                    : ValidationError.OrphanBlock;
            }
            return ValidationError.OrphanBlock;
        }
    }
}