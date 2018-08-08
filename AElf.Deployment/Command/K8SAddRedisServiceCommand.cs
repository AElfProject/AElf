using System.Collections.Generic;
using AElf.Deployment.Helper;
using AElf.Deployment.Models;
using k8s.Models;

namespace AElf.Deployment.Command
{
    public class K8SAddRedisServiceCommand:IDeployCommand
    {
        public void Action(DeployArgument arg)
        {
            var body = new V1Service
            {
                Metadata = new V1ObjectMeta
                {
                    Name = "service-redis",
                    Labels = new Dictionary<string, string>
                    {
                        {"name", "service-redis"}
                    }
                },
                Spec = new V1ServiceSpec
                {
                    Ports = new List<V1ServicePort>
                    {
                        new V1ServicePort(7001)
                    },
                    Selector = new Dictionary<string, string>
                    {
                        {"name", "set-redis"}
                    },
                    ClusterIP = "None"
                }
            };

            K8SRequestHelper.CreateNamespacedService(body, "default");
        }
    }
}