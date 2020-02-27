using Google.Protobuf;

namespace AElf.Kernel
{
    public static class BlockStateSetExtension
    {
        public static bool TryGetValue(this BlockStateSet blockStateSet, string key, out ByteString value)
        {
            value = null;
            if (blockStateSet.Deletes.Contains(key))
            {
                return true;
            }

            if (blockStateSet.Changes.ContainsKey(key))
            {
                value = blockStateSet.Changes[key];
                return true;
            }
            
            if (blockStateSet.BlockExecutedCache.ContainsKey(key))
            {
                value = blockStateSet.BlockExecutedCache[key];
                return true;
            }

            return false;
        }
    }
}