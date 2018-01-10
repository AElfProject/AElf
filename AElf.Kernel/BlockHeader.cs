using System;

namespace AElf.Kernel
{
    public class BlockHeader : IBlockHeader
    {
        public int Version => 0;
        public Hash PreBlockHash { get; protected set; }
        public Hash MerkleRootHash { get; protected set; }
        public long TimeStamp { get; protected set; }
        /// <summary>
        /// The difficulty of mining.
        /// </summary>
        public int Bits => GetBits();
        /// <summary>
        /// Random value.
        /// </summary>
        public int Nonce => GetNonce();

        public void AddTransaction(IHash<ITransaction> hash)
        {
            throw new NotImplementedException();
        }

        public IHash<IMerkleTree<ITransaction>> GetTransactionMerkleTreeRoot()
        {
            throw new NotImplementedException();
        }

        private int GetBits()
        {
            return 1;
        }

        private int GetNonce()
        {
            return new Random().Next(1, 100);
        }
    }
}