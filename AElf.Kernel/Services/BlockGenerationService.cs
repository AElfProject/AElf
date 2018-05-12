using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Kernel.Managers;
using QuickGraph.Collections;

namespace AElf.Kernel.Services
{
    public class BlockGenerationService : IBlockGenerationService
    {
        private readonly IWorldStateManager _worldStateManager;

        public BlockGenerationService(IWorldStateManager worldStateManager)
        {
            _worldStateManager = worldStateManager;
        }
        
        /// <inheritdoc/>
        public async Task<Block> BlockGeneration(Hash chainId, Hash lastBlockHash, IEnumerable<Hash> txIds)
        {
            var block = new Block(lastBlockHash);
            
            foreach (var txId in txIds)
            {
                block.AddTransaction(txId);
            }
            
            block.FillTxsMerkleTreeRootInHeader();
            var ws = await _worldStateManager.GetWorldStateAsync(chainId, lastBlockHash);
            if(ws != null)
                block.Header.MerkleTreeRootOfWorldState = await ws.GetWorldStateMerkleTreeRootAsync();
            block.Body.BlockHeader = block.Header.GetHash();
            
            
            return block;
        }
        
        /// <inheritdoc/>
        public async Task<BlockHeader> BlockHeaderGeneration(Hash chainId, Hash lastBlockHash, Hash merkleTreeRootForTransaction)
        {
            var ws = await _worldStateManager.GetWorldStateAsync(chainId, lastBlockHash);
            var state = await ws.GetWorldStateMerkleTreeRootAsync();
            var header = new BlockHeader
            {
                Version = 0,
                PerviousBlock = lastBlockHash,
                MerkleTreeRootOfWorldState = state,
                MerkleTreeRootOfTransactions = merkleTreeRootForTransaction
            };

            return header;
        }
    }
}