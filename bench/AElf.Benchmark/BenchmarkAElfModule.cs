using AElf.Modularity;
using AElf.OS;
using AElf.Kernel.SmartContract.Parallel;
using Volo.Abp.Modularity;

namespace AElf.Benchmark
{
    [DependsOn(
        typeof(OSCoreWithChainTestAElfModule)
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

    [DependsOn(
        typeof(OSCoreWithChainTestAElfModule),
        typeof(ParallelExecutionModule)
    )]
    public class BenchmarkParallelAElfModule : AElfModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
        }
    }
}