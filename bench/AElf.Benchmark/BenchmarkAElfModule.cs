using AElf.Database;
using AElf.Kernel.Infrastructure;
using AElf.Kernel.SmartContract.Parallel;
using AElf.Modularity;
using AElf.OS;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.Modularity;

namespace AElf.Benchmark
{
    [DependsOn(
        typeof(OSCoreWithChainTestAElfModule),
        typeof(ParallelExecutionModule)
    )]
    public class BenchmarkAElfModule : AElfModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            var services = context.Services;

//            services.AddKeyValueDbContext<BlockchainKeyValueDbContext>(p =>
//            {
//                var dbConnectionString = services.GetConfiguration().GetSection("ConnectionStrings:BlockchainDb").Value;
//
//                if (string.IsNullOrWhiteSpace(dbConnectionString))
//                {
//                    p.UseInMemoryDatabase();
//                }
//                else
//                {
//                    p.UseRedisDatabase();
//                }
//            });
//            
//            services.AddKeyValueDbContext<StateKeyValueDbContext>(p =>
//            {
//                var dbConnectionString = services.GetConfiguration().GetSection("ConnectionStrings:StateDb").Value;
//
//                if (string.IsNullOrWhiteSpace(dbConnectionString))
//                {
//                    p.UseInMemoryDatabase();
//                }
//                else
//                {
//                    p.UseRedisDatabase();
//                }
//            });
        }
    }
}