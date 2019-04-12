namespace AElf.Database
{
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