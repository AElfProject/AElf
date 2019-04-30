using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;

namespace AElf.Database
{
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
}