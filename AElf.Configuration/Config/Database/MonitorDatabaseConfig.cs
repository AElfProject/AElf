namespace AElf.Configuration
{
    [ConfigFile(FileName = "monitor-database.json")]
    public class MonitorDatabaseConfig:ConfigBase<MonitorDatabaseConfig>
    {
        public string Url { get; set; }

        public string Username { get; set; }

        public string Password { get; set; }
    }
}