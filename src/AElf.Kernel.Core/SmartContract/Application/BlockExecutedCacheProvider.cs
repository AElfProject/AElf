using System.Collections.Generic;
using Google.Protobuf;

namespace AElf.Kernel.SmartContract.Application
{
    public abstract class BlockExecutedCacheProvider
    {
        protected abstract string GetBlockExecutedDataName();
            
        protected string GetBlockExecutedCacheKey(IMessage key = null)
        {
            var list = new List<string> {KernelConstants.BlockExecutedCacheKey, GetBlockExecutedDataName()};
            if(key != null) list.Add(key.ToString());
            return string.Join("/", list);
        }
    }
}