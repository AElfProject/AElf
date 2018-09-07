using System.Collections.Generic;

namespace AElf.Configuration.Config.Network
{
    [ConfigFile(FileName = "network.json")]
    public class NetworkConfig : ConfigBase<NetworkConfig>
    {
        public List<string> Bootnodes { get; set; }
        
        public List<string> Peers { get; set; }

        public string PeersDbPath { get; set; }
        
        /// <summary>
        /// Server listening Port
        /// </summary>
        public int ListeningPort { get; set; }
    }
}