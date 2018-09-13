using System.Collections.Generic;
using AElf.Cryptography.ECDSA;

namespace AElf.Configuration.Config.Network
{
    [ConfigFile(FileName = "network.json")]
    public class NetworkConfig : ConfigBase<NetworkConfig>
    {
        public List<string> Bootnodes { get; set; }
        
        public List<string> Peers { get; set; }

        public string PeersDbPath { get; set; }
        
        /// <summary>
        /// SideChainServer listening Port
        /// </summary>
        public int ListeningPort { get; set; }
        
        public ECKeyPair EcKeyPair { get; set; }
    }
}