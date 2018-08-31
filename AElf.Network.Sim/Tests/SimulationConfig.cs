using System.Collections.Generic;
using AElf.Network.Sim.Node;
using Newtonsoft.Json;

namespace AElf.Network.Sim.Tests
{
    public class SimulationConfig
    {
        [JsonProperty("nodes")]
        public List<SimNodeConfiguration> Nodes { get; set; }
    }
}