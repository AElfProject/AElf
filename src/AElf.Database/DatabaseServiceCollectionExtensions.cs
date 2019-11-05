using System;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Volo.Abp.Data;

namespace AElf.Database
{
    public static class DatabaseServiceCollectionExtensions
    {
        public static IServiceCollection AddKeyValueDbContext<TKeyValueDbContext>(
            this IServiceCollection serviceCollection,
            Action<KeyValueDbContextCreationOptions<TKeyValueDbContext>> builder = null)
            where TKeyValueDbContext : KeyValueDbContext<TKeyValueDbContext>
        {
            serviceCollection.TryAddSingleton<TKeyValueDbContext>();

            serviceCollection.TryAddTransient<KeyValueDatabaseOptions<TKeyValueDbContext>>(factory =>
            {
                var o = new KeyValueDatabaseOptions<TKeyValueDbContext>();
                var name = ConnectionStringNameAttribute.GetConnStringName<TKeyValueDbContext>();

                o.ConnectionString = factory.GetRequiredService<IConnectionStringResolver>().Resolve(name);

                return o;
            });


            var options = new KeyValueDbContextCreationOptions<TKeyValueDbContext>(serviceCollection);

            builder?.Invoke(options);

            return serviceCollection;
        }
    }
}