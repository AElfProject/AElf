using AElf.Modularity;
using Volo.Abp.Modularity;

namespace AElf.Management.Website
{
    [DependsOn(typeof(ManagementAElfModule))]
    // ReSharper disable once ClassNeverInstantiated.Global
    public class ManagementWebsiteAElfModule : AElfModule
    {
        
    }
}