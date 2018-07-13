using System.Collections.Generic;
using AElf.Configuration;
using Newtonsoft.Json;

namespace AElf.Kernel.Node.Config
{
    public class MinersInfo : ConfigBase<MinersInfo>
    {
        [JsonProperty("producers")]
        public Dictionary<string, Dictionary<string, string>> Producers { get; set; }

        public MinersInfo()
        {
            Producers = new Dictionary<string, Dictionary<string, string>>
            {
                {
                    "1", new Dictionary<string, string>
                    {
                        {"address", "0x04e4bb019b3d8eb89c12a32ede9f82c77c24"}
                    }
                },
                {
                    "2", new Dictionary<string, string>
                    {
                        {"address", "0x04d977d9e16e6c12055da9fc5d9eca287829"}
                    }
                },
                {
                    "3", new Dictionary<string, string>
                    {
                        {"address", "0x04b478c4e2e7ff66003f095acdaae2fcfef5"}
                    }
                }
            };
        }
    }
}

