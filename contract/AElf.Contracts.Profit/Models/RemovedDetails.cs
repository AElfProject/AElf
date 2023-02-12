using System.Collections.Generic;
using AElf.CSharp.Core;

namespace AElf.Contracts.Profit
{
    public class RemovedDetails : Dictionary<long, long>
    {
        public void TryAdd(long key, long value)
        {
            if (ContainsKey(key))
            {
                this[key] = this[key].Add(value);
            }
            else
            {
                this[key] = value;
            }
        }
    }
}