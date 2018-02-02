using AElf.Kernel.Extensions;
using System;

namespace AElf.Kernel
{
    [Serializable]
    public class Block : IBlock
    {
        private BlockHeader BlockHeader { get; set; }

        private BlockBody BlockBody { get; set; } = new BlockBody();

        /// <summary>
        /// Initializes a new instance of the <see cref="T:AElf.Kernel.Block"/> class.
        /// a previous block must be referred, except the genesis block.
        /// </summary>
        /// <param name="preBlockHash">Pre block hash.</param>
        public Block(Hash<IBlock> preBlockHash)
        {
            BlockHeader = new BlockHeader(preBlockHash);
        }

        /// <summary>
        /// Adds the transaction to the block and wait for transaction execution
        /// </summary>
        /// <returns><c>true</c>, if transaction was added, <c>false</c> otherwise.</returns>
        /// <param name="tx">Tx.</param>
        public bool AddTransaction(ITransaction tx)
        {
            if (BlockBody.AddTransaction(tx))
            {
                BlockHeader.AddTransaction(tx.GetHash());
                return true;
            }
            return false;
        }

        /// <summary>
        /// Gets the body, which contains all the transactions in the block.
        /// </summary>
        /// <returns>The body.</returns>
        public IBlockBody GetBody()
        {
            return BlockBody;
        }

        /// <summary>
        /// Gets the header, which contains all the necessary information
        /// for SPV.
        /// </summary>
        /// <returns>The header.</returns>
        public IBlockHeader GetHeader()
        {
            return BlockHeader;
        }

        /// <summary>
        /// Returns the block hash.
        /// </summary>
        /// <returns>The hash.</returns>
        public IHash GetHash()
        {
            return new Hash<IBlock>(this.CalculateHash());
        }
    }
}
