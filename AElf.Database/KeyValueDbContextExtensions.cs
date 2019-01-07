namespace AElf.Database
{
    public static class KeyValueDbContextExtensions
    {
        public static IKeyValueCollection<byte[]> Collection(
            this IKeyValueDbContext keyValueDbContext, string name)
        {
            return keyValueDbContext.Collection<byte[]>(name);
        }
    }
}