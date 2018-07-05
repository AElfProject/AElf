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
                        {"address", "0439be531445c62dbc7f8149e17d22b0c7cd"}
                    }
                },
                {
                    "2", new Dictionary<string, string>
                    {
                        {"address", "04a2b651f9ff03d83ef55175b5c34f1afa00"}
                    }
                },
                {
                    "3", new Dictionary<string, string>
                    {
                        {"address", "049fb7b49998d6febf0dd5c9095abf4c2942"}
                    }
                }
            };
        }
    }
}

