using AElf.BenchBase;
using Volo.Abp.Modularity;

namespace AElf.Database.Benches
{
    public class DatabaseBenchAElfModule : BenchBaseAElfModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            context.Services.AddKeyValueDbContext<DbContext>(o => o.UseInMemoryDatabase());
        }
    }
}