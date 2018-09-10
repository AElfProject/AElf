using System.Collections.Generic;
using System.IO;
using AElf.Common.Application;
using AElf.Cryptography.Certificate;
using AElf.Management.Helper;
using AElf.Management.Models;
using k8s;
using k8s.Models;

namespace AElf.Management.Commands
{
    public class K8SGrpcKeyCommand:IDeployCommand
    {
        public void Action(DeployArg arg)
        {
            var body = new V1ConfigMap
            {
                ApiVersion = V1ConfigMap.KubeApiVersion,
                Kind = V1ConfigMap.KubeKind,
                Metadata = new V1ObjectMeta
                {
                    Name = GlobalSetting.KeysConfigName,
                    NamespaceProperty = arg.SideChainId
                },
                Data = GetAndCreateGrpcKey(arg)
            };

            K8SRequestHelper.GetClient().CreateNamespacedConfigMap(body, arg.SideChainId);
            
            if (!arg.IsDeployMainChain)
            {
                // update main chain config
                
            }

            throw new System.NotImplementedException();
        }

        private Dictionary<string, string> GetAndCreateGrpcKey(DeployArg arg)
        {
            var result = new Dictionary<string, string>();
            
            var certificateStore = new CertificateStore(ApplicationHelpers.GetDefaultDataDir());
            certificateStore.WriteKeyAndCertificate(arg.SideChainId, arg.LauncherArg.ClusterIp);

            var certFileName = Path.Combine(ApplicationHelpers.GetDefaultDataDir(), "certs", arg.SideChainId + ".cert.pem");
            var cert = File.ReadAllText(certFileName);
            var keyFileName = Path.Combine(ApplicationHelpers.GetDefaultDataDir(), "keys", arg.SideChainId + ".key.pem");
            var key = File.ReadAllText(keyFileName);
            
            result.Add(certFileName,cert);
            result.Add(keyFileName, key);

            return result;
        }
    }
}