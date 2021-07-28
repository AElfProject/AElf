using System.Collections.Generic;
using System.Linq;
using AElf.Standards.ACS0;
using AElf.Types;

namespace AElf.Kernel.CodeCheck
{
    internal class ContractCodeHashMap : Dictionary<long, ContractCodeHashList>
    {
        public void TryAdd(long blockHeight, Hash codeHash)
        {
            if (ContainsKey(blockHeight))
            {
                var value = this[blockHeight];
                value.Value.Add(codeHash);
                this[blockHeight] = value;
            }
            else
            {
                this[blockHeight] = new ContractCodeHashList
                {
                    Value = { codeHash }
                };
            }
        }

        public bool ContainsValue(Hash codeHash)
        {
            return Values.SelectMany(l => l.Value).Contains(codeHash);
        }

        public void RemoveValuesBeforeLibHeight(long libHeight)
        {
            foreach (var key in Keys.Where(k => k <= libHeight))
            {
                Remove(key);
            }
        }
    }
}