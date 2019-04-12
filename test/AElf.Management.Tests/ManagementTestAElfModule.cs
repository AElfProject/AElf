using System.Collections.Generic;
using AElf.Management.Interfaces;
using AElf.Management.Services;
using AElf.Modularity;
using AElf.TestBase;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.Modularity;

namespace AElf.Management.Tests
{
    [DependsOn(typeof(ManagementAElfModule),
        typeof(TestBaseAElfModule))]
    public class ManagementTestAElfModule : AElfModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            Configure<ManagementOptions>(o =>
            {
                o.DeployType = "k8s";
                o.ServiceUrls = new Dictionary<string, ServiceUrl>
                    {{"AElf", new ServiceUrl {RpcAddress = "http://127.0.0.1:8000"}}};
            });

            Configure<MonitorDbOptions>(o =>
            {
                o.Url = "http://localhost:8086";
                o.Username = "root";
                o.Password = "root";
            });
        }
    }
}