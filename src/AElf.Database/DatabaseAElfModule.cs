using System.Dynamic;
using AElf.Modularity;
using Volo.Abp.Modularity;

namespace AElf.Database
{
    [DependsOn(typeof(CoreAElfModule),typeof(Volo.Abp.Data.AbpDataModule))]
    public class DatabaseAElfModule : AElfModule
    {
    }
}