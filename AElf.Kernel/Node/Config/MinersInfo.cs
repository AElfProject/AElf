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
                        {"pubkey", "BMdbNxD7u7Hd9lkYGOC5nFT1"}
                    }
                },
                {
                    "2", new Dictionary<string, string>
                    {
                        {"coinbase", "KAOFepo=="},
                        {"pubkey", "BMdbNxD7u7Hd9lkYGOC5nFTl"}
                    }
                },
                {
                    "3", new Dictionary<string, string>
                    {
                        {"coinbase", "FfEpvVG!="},
                        {"pubkey", "BMdbNxD7u7Hd9lkYGOC5nFT2"}
                    }
                }
            };
        }
    }
}