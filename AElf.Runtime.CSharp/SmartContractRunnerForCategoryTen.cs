using System.Collections.Generic;
using System.Runtime.Loader;
using AElf.Kernel.SmartContract.Infrastructure;

namespace AElf.Runtime.CSharp
{
    public class SmartContractRunnerForCategoryTen : SmartContractRunnerForCategoryTwo
    {
        public SmartContractRunnerForCategoryTen(string sdkDir, IServiceContainer<IExecutivePlugin> executivePlugins,
            IEnumerable<string> blackList = null, IEnumerable<string> whiteList = null) : base(sdkDir, executivePlugins,
            blackList, whiteList)
        {
            Category = 10;
        }

        protected override AssemblyLoadContext GetLoadContext()
        {
            return AssemblyLoadContext.Default;
        }
    }
}