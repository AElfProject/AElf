using System.Collections.Generic;

namespace AElf.Configuration.Config.Management
{
    [ConfigFile(FileName = "api-key.json",IsWatch = true)]
    public class ApiKeyConfig:ConfigBase<ApiKeyConfig>
    {
        public Dictionary<string, string> ChainKeys { get; set; }
    }
}