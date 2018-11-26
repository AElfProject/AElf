using System.Collections.Generic;
using AElf.Management.Helper;
using AElf.Management.Models;
using k8s;
using k8s.Models;

namespace AElf.Management.Commands
{
    public class K8SAddChainInfoCommand : IDeployCommand
    {
        public void Action(DeployArg arg)
        {
            var body = new V1ConfigMap
            {
                ApiVersion = V1ConfigMap.KubeApiVersion,
                Kind = V1ConfigMap.KubeKind,
                Metadata = new V1ObjectMeta
                {
                    Name = GlobalSetting.ChainInfoConfigName,
                    NamespaceProperty = arg.SideChainId
                },
                Data = new Dictionary<string, string>
                {
                    {"chain.json", "{\"id\":\"" + arg.SideChainId + "\"}"}
                }
            };

            K8SRequestHelper.GetClient().CreateNamespacedConfigMap(body, arg.SideChainId);
        }
    }
}