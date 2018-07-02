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
                        {"address", "04fdf7d50f69be44a55a01f22d5910a96ddf"}
                    }
                },
                {
                    "2", new Dictionary<string, string>
                    {
                        {"address", "0479ba369f30c315db014dd273cfe4ca576e"}
                    }
                },
                {
                    "3", new Dictionary<string, string>
                    {
                        {"address", "0463eee8df29d238712e63245a41cc0d4b45"}
                    }
                }
            };
        }
    }
}

