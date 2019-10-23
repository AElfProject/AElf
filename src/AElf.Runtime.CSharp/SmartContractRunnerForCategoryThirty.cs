using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using System.Threading.Tasks;
using AElf.Kernel;
using AElf.Kernel.SmartContract.Infrastructure;
using AElf.Types;

namespace AElf.Runtime.CSharp
{
    public class SmartContractRunnerForCategoryThirty : SmartContractRunnerForCategoryZero
    {
        public SmartContractRunnerForCategoryThirty(string sdkDir, IServiceContainer<IExecutivePlugin> executivePlugins,
            IEnumerable<string> blackList = null, IEnumerable<string> whiteList = null) : base(sdkDir, executivePlugins,
            blackList, whiteList)
        {
            Category = KernelConstants.CodeCoverageRunnerCategory;
        }

        public override async Task<IExecutive> RunAsync(SmartContractRegistration reg)
        {
            var code = reg.Code.ToByteArray();
            var executive = new Executive(_executivePlugins, _sdkStreamManager) { ContractHash = reg.CodeHash };
            executive.Load(code);

            return await Task.FromResult(executive);
        }
    }
}