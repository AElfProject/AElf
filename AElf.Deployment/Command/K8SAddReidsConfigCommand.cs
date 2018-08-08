using System;
using System.Collections.Generic;
using AElf.Deployment.Helper;
using AElf.Deployment.Models;
using k8s.Models;

namespace AElf.Deployment.Command
{
    public class K8SAddReidsConfigCommand : IDeployCommand
    {
        public void Action(DeployArgument arg)
        {
            var body = new V1ConfigMap
            {
                ApiVersion = V1ConfigMap.KubeApiVersion,
                Kind = V1ConfigMap.KubeKind,
                Metadata = new V1ObjectMeta
                {
                    Name = "config-redis",
                    NamespaceProperty = "default"
                },
                Data = new Dictionary<string, string>
                {
                    {
                        "config-redis",
                        string.Concat("port 7001", Environment.NewLine, "bind 0.0.0.0", Environment.NewLine, "appendonly no", Environment.NewLine)
                    }
                }
            };

            K8SRequestHelper.CreateNamespacedConfigMap(body, "default");
        }
    }
}