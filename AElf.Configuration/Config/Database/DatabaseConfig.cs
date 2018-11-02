using System.Collections.Generic;
using AElf.Common.Enums;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace AElf.Configuration
{
    [ConfigFile(FileName = "database.json")]
    public class DatabaseConfig : ConfigBase<DatabaseConfig>
    {
        [JsonConverter(typeof(StringEnumConverter))]
        public DatabaseType Type { get; set; }

        public Dictionary<string, DatabaseHost> Hosts { get; set; }
    }

    public class DatabaseHost
    {
        public string Host { get; set; }
        
        public int Port { get; set; }
        
        public int Number { get; set; }
    }
}