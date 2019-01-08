namespace AElf.Database
{
    public class KeyValueDatabaseOptions<TKeyValueDbContext>
        where TKeyValueDbContext : KeyValueDbContext<TKeyValueDbContext>
    {
        public string ConnectionString { get; set; }
    }
}