namespace AElf.Database
{
    public class SsdbDatabase<TKeyValueDbContext> : RedisDatabase<TKeyValueDbContext>
        where TKeyValueDbContext:KeyValueDbContext<TKeyValueDbContext>
    {
        public SsdbDatabase(KeyValueDatabaseOptions<TKeyValueDbContext> options) : base(options)
        {
        }
    }
}