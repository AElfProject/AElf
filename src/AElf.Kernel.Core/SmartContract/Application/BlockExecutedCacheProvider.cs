using System.Collections.Generic;
using Google.Protobuf;

namespace AElf.Kernel.SmartContract.Application
{
    public abstract class BlockExecutedDataProvider
    {
        protected abstract string GetBlockExecutedDataName();
            
        protected string GetBlockExecutedDataKey(IMessage key = null)
        {
            var list = new List<string> {KernelConstants.BlockExecutedDataKey, GetBlockExecutedDataName()};
            if(key != null) list.Add(key.ToString());
            return string.Join("/", list);
        }
    }
}