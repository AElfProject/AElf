using System.Collections.Generic;
using AElf.Cryptography.ECDSA;

namespace AElf.Configuration.Config.Network
{
    [ConfigFile(FileName = "network.json")]
    public class NetworkConfig : ConfigBase<NetworkConfig>
    {
        /// <summary>
        /// This nodes key pair.
        /// </summary>
        public ECKeyPair EcKeyPair { get; set; }
            
        public List<string> Bootnodes { get; set; }
        
        public List<string> Peers { get; set; }

        public string PeersDbPath { get; set; }
        
        /// <summary>
        /// SideChainServer listening Port
        /// </summary>
        public int ListeningPort { get; set; }
        
        /// <summary>
        /// Value that determines which type of peers can connect to the node.
        /// </summary>
        public string NetAllowed { get; set; }
        
        /// <summary>
        /// The white-listed public keys when NetAllowed = Listed.
        /// </summary>
        public List<string> NetWhitelist { get; set; }
    }
}