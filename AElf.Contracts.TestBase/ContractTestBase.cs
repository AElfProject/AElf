using AElf.Database;
using AElf.Kernel.Infrastructure;
using AElf.Modularity;
using AElf.TestBase;
using Volo.Abp;

namespace AElf.Contracts.TestBase
{
    public class ContractTestBase<TModule> : AElfIntegratedTest<TModule>
        where TModule : AElfModule
    {
        
    }
}