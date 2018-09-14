using System.Collections.Generic;

namespace AElf.Configuration.Config.Management
{
    [ConfigFile(FileName = "apikey.json")]
    public class ApiKeyConfig:ConfigBase<ApiKeyConfig>
    {
        public Dictionary<string, string> ChainKeys { get; set; }
    }
}