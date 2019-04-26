using System.Collections.Generic;

namespace AElf.Kernel
{
    public partial class MerklePath
    {
        public MerklePath(IEnumerable<Hash> hashes)
        {
            Path.AddRange(hashes);
        }

        /// <summary>
        /// Calculate the <see cref="BinaryMerkleTree.Root"/> with path and provided leaf.
        /// </summary>
        /// <param name="leaf"></param>
        /// <returns></returns>
        public Hash ComputeRootWith(Hash leaf)
        {
            // TODO: remove clone
            Hash hash = leaf.Clone();
            foreach (var node in Path)
            {
                hash = BinaryMerkleTree.CalculateRootFromMultiHash(new[] {hash, node});
            }

            return hash;
        }
    }
}