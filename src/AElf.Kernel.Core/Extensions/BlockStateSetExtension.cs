namespace AElf.Kernel;

public static class BlockStateSetExtension
{
    public static bool TryGetState(this BlockStateSet blockStateSet, string key, out ByteString value)
    {
        value = null;
        if (blockStateSet.Deletes.Contains(key)) return true;

        if (blockStateSet.Changes.TryGetValue(key, out var change))
        {
            value = change;
            return true;
        }

        return false;
    }

    public static bool TryGetExecutedCache(this BlockStateSet blockStateSet, string key, out ByteString value)
    {
        value = null;

        if (blockStateSet.BlockExecutedData.ContainsKey(key))
        {
            value = blockStateSet.BlockExecutedData[key];
            return true;
        }

        return false;
    }
}