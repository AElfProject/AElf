using Newtonsoft.Json;

namespace AElf.Management.Models
{
    public class ChainHeightResult
    {
        [JsonProperty("result")] public ChainHeightResultDetail Result { get; set; }
    }

    public class ChainHeightResultDetail
    {
        [JsonProperty("block_height")] public string ChainHeight { get; set; }
    }
}