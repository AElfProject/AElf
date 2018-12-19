using System.Collections.Generic;
using System.Linq;
using AElf.Common;
using AElf.Kernel;
using AElf.Synchronization.BlockSynchronization;
using Xunit;

namespace AElf.Synchronization.Tests
{
    public class BlockSetTests
    {
        /// <summary>
        /// Builds the genesis block with AElfs builder.
        /// </summary>
        /// <returns></returns>
        private IBlock GetGenesisBlock()
        {
            var builder = new GenesisBlockBuilder().Build(Hash.Generate());
            return builder.Block;
        }
        
        /// <summary>
        /// Given a block, will generate the next, only the height and
        /// the previous will not be random.
        /// </summary>
        /// <param name="previous">The block to build upon on.</param>
        /// <returns>The new block</returns>
        private IBlock BuildNext(IBlock previous)
        {
            Assert.NotNull(previous);
            
            return new Block
            {
                Header = new BlockHeader
                {
                    Index = previous.Header.Index + 1,
                    MerkleTreeRootOfTransactions = Hash.Generate(),
                    SideChainTransactionsRoot = Hash.Generate(),
                    SideChainBlockHeadersRoot = Hash.Generate(),
                    ChainId = Hash.LoadByteArray(new byte[] {0x01, 0x02, 0x03}),
                    PreviousBlockHash = previous.GetHash(),
                    MerkleTreeRootOfWorldState = Hash.Generate()
                }
            };
        }


        /// <summary>
        /// Will create a chain from start with <see cref="count"/> extra blocks.
        /// Total block count will be <see cref="count"/>+1. 
        /// </summary>
        /// <param name="start">The start block, if null will create a genesis block.</param>
        /// <param name="count">The amount of extra blocks to create</param>
        /// <returns>return the generated chain</returns>
        public List<IBlock> GenerateChain(int count, Block start = null)
        {
            Assert.True(count > 0);
            
            List<IBlock> blocks = new List<IBlock>();
            
            IBlock current = start ?? GetGenesisBlock();
            blocks.Add(current);
            
            for (int i = 0; i < count; i++)
            {
                current = BuildNext(current);
                blocks.Add(current);
            }

            return blocks;
        }

        [Fact]
        public void PushBlock()
        {
            BlockSet blockSet = new BlockSet();
            List<IBlock> chain = GenerateChain(1);
            
            blockSet.PushBlock(chain.ElementAt(0)); // push genesis

            // push unlinkable
        }
    }
}