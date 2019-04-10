using System.Collections.Generic;

namespace AElf.OS.Network
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

        /// <summary>
        /// Timeout used when trying to connect to another peer.
        /// </summary>
        public int PeerDialTimeout { get; set; } = NetworkConsts.DefaultPeerDialTimeout;

        /// <summary>
        /// Maximum amount of values used when synchronizing a fork.
        /// </summary>
        public int BlockIdRequestCount { get; set; } = NetworkConsts.DefaultBlockRequestCount;

        /// <summary>
        /// Indicates if this node will compress blocks when a peer requests blocks.
        /// </summary>
        public bool CompressBlocksOnRequest { get; set; } = NetworkConsts.DefaultCompressBlocks;
    }
}