using System.Collections.Generic;
using System.Linq;
using AElf.Network.Data;

namespace AElf.Network.Config
{
    public class AElfNetworkConfig : IAElfNetworkConfig
    {
        public List<NodeData> Bootnodes { get; set; }
        public List<string> Peers { get; set; }
        
        public bool UseCustomBootnodes
        {
            get { return Bootnodes != null && Bootnodes.Any(); }
        }
        
        /// <summary>
        /// Server listening host
        /// </summary>
        public string Host { get; set; } = "127.0.0.1";
        
        /// <summary>
        /// Server listening Port
        /// </summary>
        public int Port { get; set; } = 6790;
    }
}