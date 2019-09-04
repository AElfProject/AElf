using System.Collections.Generic;
using AElf.OS.Network.Metrics;

namespace AElf.OS.Network.Domain
{
    public class Peer
    {
        public string Pubkey { get; set; }
        public long LastKnownLibHeight { get; set; }
        public string IpAddress { get; set; }
        public int ProtocolVersion { get; set; }
        public long ConnectionTime { get; set; }
        public bool Inbound { get; set; }
        public int BufferedTransactionsCount { get; set; }
        public int BufferedBlocksCount { get; set; }
        public int BufferedAnnouncementsCount { get; set; }
        public List<RequestMetric> RequestMetrics { get; set; }
    }
}