using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Common.Enums;
using AElf.Configuration;
using AElf.Configuration.Config.GRPC;
using AElf.Management.Helper;
using AElf.Management.Models;
using k8s;
using k8s.Models;
using Microsoft.AspNetCore.JsonPatch;
using Uri = AElf.Configuration.Config.GRPC.Uri;

namespace AElf.Management.Commands
{
    public class K8SAddConfigCommand : IDeployCommand
    {
        public async Task Action(DeployArg arg)
        {
            var body = new V1ConfigMap
            {
                ApiVersion = V1ConfigMap.KubeApiVersion,
                Kind = V1ConfigMap.KubeKind,
                Metadata = new V1ObjectMeta
                {
                    Name = GlobalSetting.CommonConfigName,
                    NamespaceProperty = arg.SideChainId
                },
                Data = new Dictionary<string, string>()
            };

            await K8SRequestHelper.GetClient().CreateNamespacedConfigMapAsync(body, arg.SideChainId);

            if (!arg.IsDeployMainChain)
            {
                var config = await K8SRequestHelper.GetClient().ReadNamespacedConfigMapAsync(GlobalSetting.CommonConfigName, arg.MainChainId);

                var grpcRemoteConfig = JsonSerializer.Instance.Deserialize<GrpcRemoteConfig>(config.Data["grpc-remote.json"]);
                grpcRemoteConfig.ChildChains.Add(arg.SideChainId, new Uri {Port = GlobalSetting.GrpcPort, Address = arg.LauncherArg.ClusterIp});
                config.Data["grpc-remote.json"] = JsonSerializer.Instance.Serialize(grpcRemoteConfig);

                var patch = new JsonPatchDocument<V1ConfigMap>();
                patch.Replace(e => e.Data, config.Data);

                await K8SRequestHelper.GetClient().PatchNamespacedConfigMapAsync(new V1Patch(patch), GlobalSetting.CommonConfigName, arg.MainChainId);
            }
        }
    }
}