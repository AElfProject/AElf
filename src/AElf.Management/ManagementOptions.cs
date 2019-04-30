using System.Collections.Generic;

namespace AElf.Management
{
    public class ManagementOptions
    {
        public string DeployType { get; set; }
        public int MonitoringInterval { get; set; }
        public Dictionary<string, ServiceUrl> ServiceUrls { get; set; }
    }
    
    public class ServiceUrl
    {
        public string RpcAddress { get; set; }

        public string MonitorRpcAddress { get; set; }
    }
}