using System.Collections.Generic;
using Newtonsoft.Json;

namespace AElf.Network.Sim.Node
{
    public class SimNodeConfiguration
    {
        [JsonProperty("listen-port")]
        public int ListeningPort { get; set; }
        
        [JsonProperty("rpc-port")]
        public int RpcPort { get; set; }
        
        public List<string> Bootnodes { get; set; }
    }
}