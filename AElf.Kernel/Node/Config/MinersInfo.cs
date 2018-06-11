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
                        {"pubkey", "MTIzNA=="}
                    }
                },
                {
                    "2", new Dictionary<string, string>
                    {
                        {"coinbase", "KAOFepo=="},
                        {"pubkey", "SrF!ve=="}
                    }
                },
                {
                    "3", new Dictionary<string, string>
                    {
                        {"coinbase", "FfEpvVG!="},
                        {"pubkey", "NTY3OA=="}
                    }
                }
            };
        }
    }
}