using System.Collections.Generic;

namespace AElf.OS.Network
{
    public interface IPeerToPeerManager
    {
        /// <summary>
        /// Discover Activites Online Nodes
        /// </summary>
        /// <returns></returns>
        IList<IPeerNodeInfo> DiscoveryNodesAsync();
    }
}