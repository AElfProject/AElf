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
                        {"pubkey", "BJHHuAdKY0pAu8ww2QtypFBr"}
                    }
                },
                {
                    "2", new Dictionary<string, string>
                    {
                        {"coinbase", "KAOFepo=="},
                        {"pubkey", "BDlxj44jQtS9Qmt+iOCVod4Y"}
                    }
                }/*,
                {
                    "3", new Dictionary<string, string>
                    {
                        {"coinbase", "FfEpvVG!="},
                        {"pubkey", "BFSux7m1cg8yZv2griHLT0J0"}
                    }
                }*/
            };
        }
    }
}