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
        public int? PeerDialTimeout { get; set; }

        /// <summary>
        /// Maximum amount of values used when synchronizing a fork.
        /// </summary>
        public int? BlockIdRequestCount { get; set; }
    }

    public class NetworksOptions : Dictionary<int, NetworkOptions>
    {
        public NetworkOptions Default
        {
            get
            {
                TryGetValue(0, out var value);
                return value;
            }
        }

        public NetworkOptions GetOrDefault(int chainId)
        {
            if (!TryGetValue(chainId, out var value))
            {
                value = Default;
            }

            return value;
        }
    }
}