using AElf.Kernel.Extensions;

namespace AElf.Kernel
{
    public partial class Block : IBlock
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="T:AElf.Kernel.Block"/> class.
        /// A previous block must be referred, except for the genesis block.
        /// </summary>
        /// <param name="preBlockHash">Pre block hash.</param>
        public Block(Hash preBlockHash)
        {
            Header = new BlockHeader(preBlockHash);
            Body = new BlockBody();
        }

        /// <summary>
        /// Adds the transactions Hash to the block
        /// </summary>
        /// <returns><c>true</c>, if the hash was added, <c>false</c> otherwise.</returns>
        /// <param name="txHash">the transactions hash</param>
        public bool AddTransaction(Hash txHash)
        {
            if (Body == null)
                Body = new BlockBody();
            
            return Body.AddTransaction(txHash);
        }

        public void FillTxsMerkleTreeRootInHeader()
        {
            Header.MerkleTreeRootOfTransactions = Body.CalculateMerkleTreeRoot();
        }

        public Hash GetHash()
        {
            return Header.GetHash();
        }
    }
}
