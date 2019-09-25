using AElf.Types;

namespace AElf.Kernel
{
    public interface IBlockIndex
    {
        Hash BlockHash { get; }
        long BlockHeight { get; }
    }

    public partial class BlockIndex : IBlockIndex
    {
        public BlockIndex(Hash hash, long height)
        {
            BlockHash = hash;
            BlockHeight = height;
        }

        public string ToDiagnosticString()
        {
            return $"[{BlockHash}: {BlockHeight}]";
        }
    }
}