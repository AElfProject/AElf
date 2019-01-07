using System;
using System.Runtime.CompilerServices;
using JetBrains.Annotations;
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

    public class KeyValueDbContextCreationOptions<TKeyValueDbContext>
        where TKeyValueDbContext : KeyValueDbContext<TKeyValueDbContext>
    {
        public KeyValueDbContextCreationOptions([NotNull] IServiceCollection services)
        {
            Services = services;
        }

        [NotNull] public IServiceCollection Services { get; }


        public KeyValueDbContextCreationOptions<TKeyValueDbContext> UseDatabase<TKeyValueDatabase>()
            where TKeyValueDatabase : class, IKeyValueDatabase<TKeyValueDbContext>
        {
            Services.AddSingleton<IKeyValueDatabase<TKeyValueDbContext>, TKeyValueDatabase>();
            return this;
        }
    }

    public static class KeyValueDbContextCreationOptionsExtensions
    {
        public static KeyValueDbContextCreationOptions<TKeyValueDbContext> UseRedisDatabase<TKeyValueDbContext>(
            this KeyValueDbContextCreationOptions<TKeyValueDbContext> creationOptions)
            where TKeyValueDbContext : KeyValueDbContext<TKeyValueDbContext>
        {
            creationOptions.UseDatabase<RedisDatabase<TKeyValueDbContext>>();
            return creationOptions;
        }
        
        public static KeyValueDbContextCreationOptions<TKeyValueDbContext> UseSsdbDatabase<TKeyValueDbContext>(
            this KeyValueDbContextCreationOptions<TKeyValueDbContext> creationOptions)
            where TKeyValueDbContext : KeyValueDbContext<TKeyValueDbContext>
        {
            creationOptions.UseDatabase<SsdbDatabase<TKeyValueDbContext>>();
            return creationOptions;
        }
        
        public static KeyValueDbContextCreationOptions<TKeyValueDbContext> UseInMemoryDatabase<TKeyValueDbContext>(
            this KeyValueDbContextCreationOptions<TKeyValueDbContext> creationOptions)
            where TKeyValueDbContext : KeyValueDbContext<TKeyValueDbContext>
        {
            creationOptions.UseDatabase<InMemoryDatabase<TKeyValueDbContext>>();
            return creationOptions;
        }
    }
    
    
}