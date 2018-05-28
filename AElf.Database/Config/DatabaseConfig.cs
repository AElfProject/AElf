namespace AElf.Database.Config
{
    public class DatabaseConfig:IDatabaseConfig
    {
        public string Type { get; }
        public string Host { get; }
        public int Port { get; }
    }
}