namespace AElf.Database.Config
{
    public interface IDatabaseConfig
    {
        DatabaseType Type { get; set; }

        string Host { get; set; }
        
        int Port { get; set; }
    }
}