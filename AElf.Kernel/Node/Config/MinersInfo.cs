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
                        {"pubkey", "BKK2Ufn/A9g+9VF1tcNPGvoB"}
                    }
                },
                {
                    "2", new Dictionary<string, string>
                    {
                        {"coinbase", "KAOFepo=="},
                        {"pubkey", "BKK2Ufn/A9g+9VF1tcNPGvoA"}
                    }
                },
                {
                    "3", new Dictionary<string, string>
                    {
                        {"coinbase", "FfEpvVG!="},
                        {"pubkey", "BKK2Ufn/A9g+9VF1tcNPGvoC"}
                    }
                }
            };
        }
    }
}