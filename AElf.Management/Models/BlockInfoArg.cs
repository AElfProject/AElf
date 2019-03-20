using Newtonsoft.Json;

namespace AElf.Management.Models
{
    public class BlockInfoArg
    {
        [JsonProperty("blockHeight")] public long BlockHeight { get; set; }

        [JsonProperty("includeTransactions")] public bool IncludeTxs { get; set; }
    }
}