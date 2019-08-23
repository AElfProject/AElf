using System;
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
        /// Indicates the types of peers that are authorized to connect to this node.
        /// Use Authorized in conjunction with <see cref="AuthorizedKeys"/>.
        /// </summary>
        public AuthorizedPeers AuthorizedPeers { get; set; } = AuthorizedPeers.Any;
        
        /// <summary>
        /// A list of allowed public keys that are authorized to connect to the node.
        /// This will be used when <see cref="AuthorizedPeers"/> is set to Authorized.
        /// </summary>
        public List<string> AuthorizedKeys { get; set; }

        /// <summary>
        /// Node Server listening Port.
        /// </summary>
        public int ListeningPort { get; set; }

        /// <summary>
        /// The maximum number of peers accepted by this node (0 for no limit).
        /// </summary>
        public int MaxPeers { get; set; } = NetworkConstants.DefaultMaxPeers;

        /// <summary>
        /// Timeout used when trying to connect to another peer.
        /// </summary>
        public int PeerDialTimeoutInMilliSeconds { get; set; } = NetworkConstants.DefaultPeerDialTimeoutInMilliSeconds;

        /// <summary>
        /// Indicates if this node will compress blocks when a peer requests blocks.
        /// </summary>
        public bool CompressBlocksOnRequest { get; set; } = NetworkConstants.DefaultCompressBlocks;
        
        /// <summary>
        /// Indicates if the node will participate in peer discovery operations.
        /// </summary>
        public bool EnablePeerDiscovery { get; set; } = true;

        /// <summary>
        /// The minimum distance between this node and peers needed to trigger initial sync. 
        /// </summary>
        public int InitialSyncOffset { get; set; } = NetworkConstants.DefaultInitialSyncOffset;
    }
    
    [Flags]
    public enum AuthorizedPeers
    {
        Any, // Any node can connect
        Authorized // Only whitelisted peers can connect
    }
}