using System.Collections.Generic;
using AElf.OS.Network;
using AElf.OS.Network.Metrics;

namespace AElf.WebApp.Application.Net.Dto
{
    public class PeerDto
    {
        public string IpAddress { get; set; }
        
        public int ProtocolVersion { get; set; }
        
        public long ConnectionTime { get; set; }
        
        public bool Inbound { get; set; }
        
        public List<RequestMetric> RequestMetrics { get; set; }
    }
}