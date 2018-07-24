namespace AElf.Configuration
{
    [ConfigFile(FileName = "database.json")]
    public class DatabaseConfig : ConfigBase<DatabaseConfig>
    {
        public DatabaseType Type { get; set; }
        public string Host { get; set; }
        public int Port { get; set; }
        public int Number { get; set; }

        public DatabaseConfig()
        {
            Type = DatabaseType.InMemory;
            Host = "127.0.0.1";
            Port = 8888;
        }
    }
}