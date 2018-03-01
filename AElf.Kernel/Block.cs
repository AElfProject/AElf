using AElf.Kernel.Extensions;
using System;

namespace AElf.Kernel
{
    [Serializable]
    public class Block : IBlock
    {
        #region Private Fileds
        private readonly BlockHeader _blockHeader;
        private readonly BlockBody _blockBody;
        #endregion

        /// <summary>
        /// Initializes a new instance of the <see cref="T:AElf.Kernel.Block"/> class.
        /// a previous block must be referred, except the genesis block.
        /// </summary>
        /// <param name="preBlockHash">Pre block hash.</param>
        public Block(IHash<IBlock> preBlockHash)
        {
            _blockHeader = new BlockHeader(preBlockHash);
            _blockBody = new BlockBody();
        }

        /// <summary>
        /// Adds the transaction to the block and wait for transaction execution
        /// </summary>
        /// <returns><c>true</c>, if transaction was added, <c>false</c> otherwise.</returns>
        /// <param name="tx">Tx.</param>
        public bool AddTransaction(ITransaction tx)
        {
            if (_blockBody.AddTransaction(tx))
            {
                _blockHeader.AddTransaction(tx.GetHash());
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
            return _blockBody;
        }

        /// <summary>
        /// Gets the header, which contains all the necessary information
        /// for SPV.
        /// </summary>
        /// <returns>The header.</returns>
        public IBlockHeader GetHeader()
        {
            return _blockHeader;
        }

        /// <summary>
        /// Returns the block hash.
        /// </summary>
        /// <returns>The hash.</returns>
        public IHash<IBlock> GetHash()
        {
            return new Hash<IBlock>(this.CalculateHash());
        }
    }
}
