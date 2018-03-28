using AElf.Kernel.Extensions;
using System;

namespace AElf.Kernel
{
    public class Block
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="T:AElf.Kernel.Block"/> class.
        /// a previous block must be referred, except the genesis block.
        /// </summary>
        /// <param name="preBlockHash">Pre block hash.</param>
        public Block(Hash preBlockHash)
        {
            Header = new BlockHeader(preBlockHash);
            Body = new BlockBody();
        }

        /// <summary>
        /// Adds the transaction to the block and wait for transaction execution
        /// </summary>
        /// <returns><c>true</c>, if transaction was added, <c>false</c> otherwise.</returns>
        /// <param name="tx">Tx.</param>
        public bool AddTransaction(Hash tx)
        {
            if (!Body.AddTransaction(tx)) 
                return false;
            Header.AddTransaction(tx);
            return true;
        }

        public BlockHeader Header { get; set; }
        public BlockBody Body { get; set; }


        /// <summary>
        /// Returns the block hash.
        /// </summary>
        /// <returns>The hash.</returns>
        public Hash GetHash()
        {
            return new Hash(this.Header.CalculateHash());
        }
    }
}
