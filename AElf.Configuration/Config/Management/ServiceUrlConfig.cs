using System.Collections.Generic;

namespace AElf.Configuration.Config.Management
{
    [ConfigFile(FileName = "serviceurl.json")]
    public class ServiceUrlConfig:ConfigBase<ServiceUrlConfig>
    {
        public Dictionary<string, ServiceUrl> ServiceUrls { get; set; }
    }

    public class ServiceUrl
    {
        public string RpcAddress { get; set; }

        public string MonitorRpcAddress { get; set; }
    }
}