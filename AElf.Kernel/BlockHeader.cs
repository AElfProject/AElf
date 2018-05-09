using AElf.Kernel.Extensions;
using AElf.Kernel.Merkle;
using System;

namespace AElf.Kernel
{
    public partial class BlockHeader : IBlockHeader, IHashProvider
    {
        /// <summary>
        /// The miner's signature.
        /// </summary>
        public byte[] Signatures;

        /// <summary>
        /// the timestamp of this block
        /// </summary>
        /// <value>The time stamp.</value>
        public long TimeStamp => (long)(DateTime.Now - new DateTime(1970, 1, 1, 0, 0, 0, 0)).TotalSeconds;

        public BlockHeader(Hash preBlockHash)
        {
            PreviousHash = preBlockHash;
        }

        public Hash PreviousHash { get; set; }

        /// <summary>
        /// block index in chain
        /// </summary>
        public ulong Index { get; set;}
        
        public Hash GetHash()
        {
            return this.CalculateHash();
        }
    }
}