using AElf.Kernel.Extensions;
using AElf.Kernel.Merkle;
using System;

namespace AElf.Kernel
{
    public partial class BlockHeader : IBlockHeader
    {
        public BlockHeader(Hash preBlockHash)
        {
            PreviousHash = preBlockHash;
        }
        
        /// <summary>
        /// The miner's signature.
        /// </summary>
        public byte[] Signatures;
        
        public Hash PreviousHash { get; set; }

        /// <summary>
        /// block index in chain
        /// </summary>
        public ulong Index { get; set;}
        
        public Hash GetHash()
        {
            return new Hash(this.CalculateHash());
        }
    }
}