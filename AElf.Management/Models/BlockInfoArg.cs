using Newtonsoft.Json;
 
 namespace AElf.Management.Models
 {
     public class BlockInfoArg
     {
         [JsonProperty("block_height")]
         public ulong BlockHeight { get; set; }
 
         [JsonProperty("include_txs")]
         public bool IncludeTxs { get; set; }
     }
 }