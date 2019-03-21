using System.Collections.Generic;
using System.Runtime.Loader;
using AElf.Kernel.SmartContract.Infrastructure;

namespace AElf.Runtime.CSharp
{
    public class SmartContractRunnerForCategoryThirty : SmartContractRunnerForCategoryThree
    {
        public SmartContractRunnerForCategoryThirty(string sdkDir, IServiceContainer<IExecutivePlugin> executivePlugins,
            IEnumerable<string> blackList = null, IEnumerable<string> whiteList = null) : base(sdkDir, executivePlugins,
            blackList, whiteList)
        {
            Category = 30;
        }
    }
}