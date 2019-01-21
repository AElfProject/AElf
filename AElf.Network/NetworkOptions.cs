using System.Collections.Generic;

namespace AElf.Network
{
    public class NetworkOptions
    {
        /// <summary>
        /// Initial set of nodes.
        /// </summary>
        public List<string> BootNodes { get; set; }
        
        /// <summary>
        /// Node Server listening Port.
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