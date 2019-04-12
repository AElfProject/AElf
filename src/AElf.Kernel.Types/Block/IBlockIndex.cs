using AElf.Common;
using Google.Protobuf;

namespace AElf.Kernel
{
    public interface IBlockIndex
    {
        Hash Hash { get; }
        long Height { get; }
    }
    
    public class BlockIndex: IBlockIndex
    {
        public BlockIndex(Hash hash, long height)
        {
            Hash = hash;
            Height = height;
        }

        public Hash Hash { get; set; }
        public long Height { get; set; }

        public BlockIndex()
        {
            
        }

        public override string ToString()
        {
            return $"[{Hash}: {Height}]";
        }
    }
}