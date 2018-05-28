using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Kernel.Managers;
using Org.BouncyCastle.Math.EC;
using QuickGraph.Collections;

namespace AElf.Kernel.Services
{
    public class BlockGenerationService : IBlockGenerationService
    {
        private readonly IWorldStateManager _worldStateManager;
        private readonly IChainManager _chainManager;
        private readonly IBlockManager _blockManager;

        public BlockGenerationService(IWorldStateManager worldStateManager, IChainManager chainManager, 
            IBlockManager blockManager)
        {
            _worldStateManager = worldStateManager;
            _chainManager = chainManager;
            _blockManager = blockManager;
        }
        
        /// <inheritdoc/>
        public async Task<IBlock> GenerateBlockAsync(Hash chainId, IEnumerable<TransactionResult> results)
        {
            var lastBlockHash = await _chainManager.GetChainLastBlockHash(chainId);
            var block = new Block(lastBlockHash);
            
            // add tx hash
            foreach (var r in results)
            {
                block.AddTransaction(r.TransactionId);
            }
            
            // calculate and set tx merkle tree root
            block.FillTxsMerkleTreeRootInHeader();
            
            // set ws merkle tree root
            await _worldStateManager.OfChain(chainId);
            
            await _worldStateManager.SetWorldStateAsync(lastBlockHash);
            var ws = await _worldStateManager.GetWorldStateAsync(lastBlockHash);
            if(ws != null)
                block.Header.MerkleTreeRootOfWorldState = await ws.GetWorldStateMerkleTreeRootAsync();
            block.Body.BlockHeader = block.Header.GetHash();
            
            // append block
            await _blockManager.AddBlockAsync(block);
            await _chainManager.AppendBlockToChainAsync(chainId, block);
            
            return block;
        }
        
        /// <inheritdoc/>
        public async Task<IBlockHeader> GenerateBlockHeaderAsync(Hash chainId, Hash merkleTreeRootForTransaction)
        {
            // get ws merkle tree root
            var lastBlockHash = await _chainManager.GetChainLastBlockHash(chainId);
            await _worldStateManager.OfChain(chainId);
            var ws = await _worldStateManager.GetWorldStateAsync(lastBlockHash);
            var state = await ws.GetWorldStateMerkleTreeRootAsync();
            
            var header = new BlockHeader
            {
                Version = 0,
                PerviousBlock = lastBlockHash,
                MerkleTreeRootOfWorldState = state,
                MerkleTreeRootOfTransactions = merkleTreeRootForTransaction
            };

            return await _blockManager.AddBlockHeaderAsync(header);
            
        }
    }
}