using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using AElf.Common.Application;
using AElf.Cryptography.Certificate;
using AElf.Management.Helper;
using AElf.Management.Models;
using k8s;
using k8s.Models;
using Microsoft.AspNetCore.JsonPatch;

namespace AElf.Management.Commands
{
    public class K8SGrpcKeyCommand : IDeployCommand
    {
        public async Task Action(DeployArg arg)
        {
            CreateGrpcKey(arg);
            var certFileName = arg.SideChainId + ".cert.pem";
            var cert = File.ReadAllText(Path.Combine(ApplicationHelper.AppDataPath, "certs", certFileName));
            var keyFileName = arg.SideChainId + ".key.pem";
            var key = File.ReadAllText(Path.Combine(ApplicationHelper.AppDataPath, "certs", keyFileName));

            var configMapData = new Dictionary<string, string> {{certFileName, cert}, {keyFileName, key}};

            if (!arg.IsDeployMainChain)
            {
                var certMainChain = await K8SRequestHelper.GetClient().ReadNamespacedConfigMapAsync(GlobalSetting.CertsConfigName, arg.MainChainId);
                var certName = arg.MainChainId + ".cert.pem";
                configMapData.Add(certName, certMainChain.Data[certName]);

                certMainChain.Data.Add(certFileName, cert);
                var patch = new JsonPatchDocument<V1ConfigMap>();
                patch.Replace(e => e.Data, certMainChain.Data);

                await K8SRequestHelper.GetClient().PatchNamespacedConfigMapAsync(new V1Patch(patch), GlobalSetting.CertsConfigName, arg.MainChainId);
            }


            var body = new V1ConfigMap
            {
                ApiVersion = V1ConfigMap.KubeApiVersion,
                Kind = V1ConfigMap.KubeKind,
                Metadata = new V1ObjectMeta
                {
                    Name = GlobalSetting.CertsConfigName,
                    NamespaceProperty = arg.SideChainId
                },
                Data = configMapData
            };

            await K8SRequestHelper.GetClient().CreateNamespacedConfigMapAsync(body, arg.SideChainId);
        }

        private void CreateGrpcKey(DeployArg arg)
        {
            var certificateStore = new CertificateStore(ApplicationHelper.AppDataPath);
            certificateStore.WriteKeyAndCertificate(arg.SideChainId, arg.LauncherArg.ClusterIp);
        }
    }
}