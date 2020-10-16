using System.Collections.Generic;
using AElf.Kernel.SmartContract;
using AElf.TestBase;
using Google.Protobuf;

namespace AElf.Kernel
{
    public class AElfKernelTestBase : AElfIntegratedTest<KernelCoreTestAElfModule>
    {
        protected string GetBlockExecutedDataKey<T>(IMessage key = null)
        {
            var list = new List<string> {KernelConstants.BlockExecutedDataKey, typeof(T).Name};
            if (key != null) list.Add(key.ToString());
            return string.Join("/", list);
        }
    }
    
    public class AElfKernelWithChainTestBase : AElfIntegratedTest<KernelCoreWithChainTestAElfModule>
    {
    }
    
    public class AElfMinerTestBase : AElfIntegratedTest<KernelMinerTestAElfModule>
    {
    }
    
    public class AccountTestBase : AElfIntegratedTest<AccountTestAElfModule>
    {
    }
}