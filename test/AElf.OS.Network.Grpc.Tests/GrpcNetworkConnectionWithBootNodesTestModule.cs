using System.Collections.Generic;
using AElf.Modularity;
using Volo.Abp.Modularity;

namespace AElf.OS.Network.Grpc
{
    [DependsOn(typeof(GrpcNetworkTestModule))]
    public class GrpcNetworkConnectionWithBootNodesTestModule: AElfModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            var services = context.Services;

            Configure<NetworkOptions>(o =>
            {
                o.ListeningPort = 2001;
                o.MaxPeers = 2;
                o.BootNodes = new List<string>
                {
                    "127.0.0.1:2020"
                };
            });
        }
    }
}