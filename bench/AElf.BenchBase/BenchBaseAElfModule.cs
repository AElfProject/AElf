using AElf.Modularity;
using AElf.TestBase;
using Volo.Abp.Modularity;

namespace AElf.BenchBase
{
    [DependsOn(typeof(TestBaseAElfModule))]
    public class BenchBaseAElfModule : AElfModule
    {
        
    }
    
    public class BenchBaseTest<TModule> : AElfIntegratedTest<TModule>
        where TModule: IAbpModule
    {
    }

    public class BenchBaseTest : AElfIntegratedTest<TestBaseAElfModule>
    {
        
    }
}