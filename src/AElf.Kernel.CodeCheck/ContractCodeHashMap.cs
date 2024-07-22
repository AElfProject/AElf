using System.Linq;

namespace AElf.Standards.ACS0;

internal partial class ContractCodeHashMap
{
    public void TryAdd(long blockHeight, Hash codeHash)
    {
        if (Value.TryGetValue(blockHeight, out var hashList))
        {
            hashList.Value.Add(codeHash);
        }
        else
        {
            Value[blockHeight] = new ContractCodeHashList
            {
                Value = { codeHash }
            };
        }
    }

    public bool ContainsValue(Hash codeHash)
    {
        return Value.Values.SelectMany(l => l.Value).Contains(codeHash);
    }

    public void RemoveValuesBeforeLibHeight(long libHeight)
    {
        foreach (var key in Value.Keys.Where(k => k <= libHeight)) Value.Remove(key);
    }
}