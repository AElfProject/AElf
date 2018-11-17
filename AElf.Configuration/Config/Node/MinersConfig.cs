using System.Collections.Generic;
using Newtonsoft.Json;

namespace AElf.Configuration
{
    [ConfigFile(FileName = "miners.json")]
    public class MinersConfig : ConfigBase<MinersConfig>
    {
        [JsonProperty("producers")]
        public Dictionary<string, Dictionary<string, string>> Producers { get; set; }
    }
}

