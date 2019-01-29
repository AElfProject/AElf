using System.Collections.Generic;

namespace AElf.Runtime.CSharp
{
    public class SmartContractRunnerForCategoryOne : SmartContractRunner
    {
        public SmartContractRunnerForCategoryOne(string sdkDir, IEnumerable<string> blackList = null,
            IEnumerable<string> whiteList = null) : base(sdkDir, blackList, whiteList)
        {
            Category = 1;
        }
    }
}