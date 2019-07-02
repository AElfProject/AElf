using System.Collections.Generic;

namespace AElf.Kernel
{
    public static class MerklePathExtensions
    {
        public static MerklePath AddRange(this MerklePath merklePath, IEnumerable<Hash> hashes)
        {
            merklePath.Path.AddRange(hashes);
            return merklePath;
        }

        /// <summary>
        /// Calculate the <see cref="BinaryMerkleTree.Root"/> with path and provided leaf.
        /// </summary>
        /// <param name="merklePath"></param>
        /// <param name="leaf"></param>
        /// <returns></returns>
        public static Hash ComputeRootWith(this MerklePath merklePath, Hash leaf)
        {
            // Done: remove clone
            Hash hash = leaf.Clone();
            foreach (var node in merklePath.Path)
            {
                hash = BinaryMerkleTree.CalculateRootFromMultiHash(new[] {hash, node});
            }

            return hash;
        }
    }
}