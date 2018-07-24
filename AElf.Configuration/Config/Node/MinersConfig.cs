using System.Collections.Generic;
using Newtonsoft.Json;

namespace AElf.Configuration
{
    [ConfigFile(FileName = "miners.json")]
    public class MinersConfig : ConfigBase<MinersConfig>
    {
        [JsonProperty("producers")]
        public Dictionary<string, Dictionary<string, string>> Producers { get; set; }

        public MinersConfig()
        {
            Producers = new Dictionary<string, Dictionary<string, string>>
            {
                {
                    "1", new Dictionary<string, string>
                    {
                        {"address", "04e4bb019b3d8eb89c12a32ede9f82c77c24"}
                    }
                },
                {
                    "2", new Dictionary<string, string>
                    {
                        {"address", "04d977d9e16e6c12055da9fc5d9eca287829"}
                    }
                },
                {
                    "3", new Dictionary<string, string>
                    {
                        {"address", "04b478c4e2e7ff66003f095acdaae2fcfef5"}
                    }
                }
            };
        }
    }
}

