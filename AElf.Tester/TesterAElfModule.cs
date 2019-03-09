using AElf.Modularity;
using AElf.OS;
using Volo.Abp.Modularity;

namespace AElf.Tester
{
    [DependsOn(typeof(OSTestAElfModule))]
    // ReSharper disable once ClassNeverInstantiated.Global
    public class TesterAElfModule : AElfModule
    {
        
    }
}