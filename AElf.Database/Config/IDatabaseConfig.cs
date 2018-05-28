namespace AElf.Database.Config
{
    public interface IDatabaseConfig
    {
        string Type { get; }

        string Host { get; }
        
        int Port { get; }
    }
}