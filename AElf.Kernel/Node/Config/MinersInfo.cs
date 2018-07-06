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
                        {"address", "0491c7b8074a634a40bbcc30d90b72a4506b"}
                    }
                },
                {
                    "2", new Dictionary<string, string>
                    {
                        {"address", "0439718f8e2342d4bd426b7e88e095a1de18"}
                    }
                },
                {
                    "3", new Dictionary<string, string>
                    {
                        {"address", "048c1ae27486084ae3a977d04edcaf4e0d3d"}
                    }
                }
            };
        }
    }
}

