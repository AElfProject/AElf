using AElf.Common;

namespace AElf.Kernel
{
    public partial class MerklePath
    {
        /// <summary>
        ///     Calculate the <see cref="BinaryMerkleTree.Root" /> with path and provided leaf.
        /// </summary>
        /// <param name="leaf"></param>
        /// <returns></returns>
        public Hash ComputeRootWith(Hash leaf)
        {
            var hash = leaf.Clone();
            foreach (var node in Path) hash = Hash.FromTwoHashes(hash, node);
            return hash;
        }
    }
}