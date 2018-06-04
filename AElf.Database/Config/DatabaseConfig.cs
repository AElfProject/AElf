namespace AElf.Database.Config
{
    public class DatabaseConfig:IDatabaseConfig
    {
        public DatabaseType Type { get; set; }
        public string Host { get; set; }
        public int Port { get; set; }

        public DatabaseConfig()
        {
            Type = DatabaseType.KeyValue;
            Host = "127.0.0.1";
            Port = 8888;
        }
    }
}