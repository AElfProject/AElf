using System.Linq;

namespace AElf.Standards.ACS0;

internal partial class ContractCodeHashMap
{
    public void TryAdd(long blockHeight, Hash codeHash)
    {
        if (Value.ContainsKey(blockHeight))
        {
            var hashList = Value[blockHeight];
            hashList.Value.Add(codeHash);
            Value[blockHeight] = hashList;
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