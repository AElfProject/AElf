using AElf.Modularity;
using AElf.TestBase;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Volo.Abp.Data;
using Volo.Abp.Modularity;

namespace AElf.Database.Tests
{
    [DependsOn(typeof(DatabaseAElfModule), typeof(TestBaseAElfModule))]
    public class TestsDatabaseAElfModule : AElfModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            var services = context.Services;
            services.AddSingleton<IConnectionStringResolver>(o => Mock.Of<IConnectionStringResolver>(
                c => c.Resolve("Default") == "127.0.0.1" &&
                     c.Resolve("AElf.Database.Tests.MyContext2") == "10.1.1.1"
            ));

            services.AddKeyValueDbContext<MyContext>(builder => builder.UseRedisDatabase());
            
            services.AddKeyValueDbContext<MyContext>(builder => builder.UseInMemoryDatabase());

            services.AddKeyValueDbContext<MyContext2>(builder => builder.UseSsdbDatabase());
            services.AddKeyValueDbContext<InMemoryDbContext>(builder => builder.UseInMemoryDatabase());

        }
    }


    [ConnectionStringName("Default")]
    public class MyContext : KeyValueDbContext<MyContext>
    {
    }


    public class MyContext2 : KeyValueDbContext<MyContext2>
    {
    }

    public class InMemoryDbContext : KeyValueDbContext<InMemoryDbContext>
    {
        
    }
}