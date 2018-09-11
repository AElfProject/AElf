using System.Collections.Generic;
using System.IO;
using AElf.Common.Application;
using AElf.Cryptography.Certificate;
using AElf.Management.Helper;
using AElf.Management.Models;
using k8s;
using k8s.Models;
using Microsoft.AspNetCore.JsonPatch;

namespace AElf.Management.Commands
{
    public class K8SGrpcKeyCommand:IDeployCommand
    {
        public void Action(DeployArg arg)
        {
            CreateGrpcKey(arg);
            var certFileName = arg.SideChainId + ".cert.pem";
            var cert = File.ReadAllText(Path.Combine(ApplicationHelpers.GetDefaultDataDir(), "certs", certFileName));
            var keyFileName = arg.SideChainId + ".key.pem";
            var key = File.ReadAllText(Path.Combine(ApplicationHelpers.GetDefaultDataDir(), "certs", keyFileName));

            var body = new V1ConfigMap
            {
                ApiVersion = V1ConfigMap.KubeApiVersion,
                Kind = V1ConfigMap.KubeKind,
                Metadata = new V1ObjectMeta
                {
                    Name = GlobalSetting.CertsConfigName,
                    NamespaceProperty = arg.SideChainId
                },
                Data = new Dictionary<string, string> {{certFileName, cert}, {keyFileName, key}}
            };

            K8SRequestHelper.GetClient().CreateNamespacedConfigMap(body, arg.SideChainId);
            
            if (!arg.IsDeployMainChain)
            {
                // update main chain config
                var patch = new JsonPatchDocument<V1ConfigMap>();
                patch.Add(e => e.Data, new Dictionary<string, string>{{certFileName, cert}});

                K8SRequestHelper.GetClient().PatchNamespacedConfigMap(new V1Patch(patch), GlobalSetting.CertsConfigName, arg.MainChainId);
            }
        }

        private void CreateGrpcKey(DeployArg arg)
        {
            var certificateStore = new CertificateStore(ApplicationHelpers.GetDefaultDataDir());
            certificateStore.WriteKeyAndCertificate(arg.SideChainId, arg.LauncherArg.ClusterIp);
        }
    }
}