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
                        {"address", "04e7e7ffc0a295536399f44cc76c6c66d32c"}
                    }
                },
                {
                    "2", new Dictionary<string, string>
                    {
                        {"address", "0445a92602e830f0499766472e76eade0a86"}
                    }
                },
                {
                    "3", new Dictionary<string, string>
                    {
                        {"address", "04c5bc7b8f47db864c57d1c83fef82c2060d"}
                    }
                }
            };
        }
    }
}

