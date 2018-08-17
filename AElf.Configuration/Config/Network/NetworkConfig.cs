using System.Collections.Generic;
using System.Linq;

namespace AElf.Configuration.Config.Network
{
    [ConfigFile(FileName = "network.json")]
    public class NetworkConfig: ConfigBase<NetworkConfig>
    {
        public List<string> Bootnodes { get; set; }
        
        public List<string> Peers { get; set; }

        public string PeersDbPath { get; set; }

        //public bool UseCustomBootnodes
        //{
        //    get { return Bootnodes != null && Bootnodes.Any(); }
        //}
        
        ///// <summary>
        ///// Server listening host
        ///// </summary>
        //public string Host { get; set; } = "127.0.0.1";
        
        /// <summary>
        /// Server listening Port
        /// </summary>
        public int ListeningPort { get; set; }

        //public int MaxPeers { get; set; } = 500;
    }
}