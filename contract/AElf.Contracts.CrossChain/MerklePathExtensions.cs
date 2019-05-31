using System.Collections.Generic;
using Acs7;
using AElf.Types;

namespace AElf.Contracts.CrossChain
{
    public static class MerklePathExtensions
    {
        public static MerklePath AddRange(this MerklePath merklePath, IEnumerable<Hash> hashes)
        {
            merklePath.Path.AddRange(hashes);
            return merklePath;
        }

        /// <summary>
        /// Calculate the merkle tree root with path and provided leaf.
        /// </summary>
        /// <param name="merklePath"></param>
        /// <param name="leaf"></param>
        /// <returns></returns>
        public static Hash ComputeRootWith(this MerklePath merklePath, Hash leaf)
        {
            return merklePath.Path.ComputeBinaryMerkleTreeRootWithPathAndLeafNode(leaf);
        }
    }
}