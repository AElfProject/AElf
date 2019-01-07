using System.Dynamic;
using AElf.Modularity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Volo.Abp.Data;
using Volo.Abp.Modularity;

namespace AElf.Database
{
    public class DatabaseAElfModule : AElfModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            var services = context.Services;
        }
    }

    public static class DatabaseServiceCollectionExtensions
    {
        public static IServiceCollection AddKeyValueDbContext<TKeyValueDbContext>(
            this IServiceCollection serviceCollection)
            where TKeyValueDbContext : class, IKeyValueDbContext, new()
        {
            serviceCollection.TryAddSingleton<TKeyValueDbContext>(o =>
            {
                var connStringName = ConnectionStringNameAttribute.GetConnStringName<TKeyValueDbContext>();
                var serviceScope = o.CreateScope();
                
            });

            return serviceCollection;
        }
    }
}