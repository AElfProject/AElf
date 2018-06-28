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
                        {"coinbase", "GdsgFf\\="},
                        {"pubkey", "0491c7b8074a634a40bbcc30d90b72a4506b"}
                    }
                },
                {
                    "2", new Dictionary<string, string>
                    {
                        {"coinbase", "KAOFepo=="},
                        {"pubkey", "0439718f8e2342d4bd426b7e88e095a1de18"}
                    }
                },
                {
                    "3", new Dictionary<string, string>
                    {
                        {"coinbase", "FfEpvVG!="},
                        {"pubkey", "0454aec7b9b5720f3266fda0ae21cb4f4274"}
                    }
                }
            };
        }
    }
}
