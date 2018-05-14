using AElf.Kernel.Extensions;
using System;
using Google.Protobuf;

// ReSharper disable once CheckNamespace
namespace AElf.Kernel
{
    public partial class BlockHeader : IBlockHeader
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

        public Hash GetHash()
        {
            return this.CalculateHash();
        }

        public byte[] Serialize()
        {
            return this.ToByteArray();
        }
    }
}