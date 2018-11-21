using System;
using System.Collections.Generic;
using System.IO;
using AElf.Common;
using AElf.Common.Application;
using AElf.Configuration.Config.Chain;
using AElf.Cryptography;
using AElf.Management.Helper;
using AElf.Management.Models;
using Base58Check;
using k8s;
using k8s.Models;

namespace AElf.Management.Commands
{
    public class K8SAddAccountKeyCommand : IDeployCommand
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
                Data = GetAndCreateAccountKey(arg)
            };

            K8SRequestHelper.GetClient().CreateNamespacedConfigMap(body, arg.SideChainId);
        }

        private Dictionary<string, string> GetAndCreateAccountKey(DeployArg arg)
        {
            if (string.IsNullOrWhiteSpace(arg.ChainAccount))
            {
                var keyStore = new AElfKeyStore(ApplicationHelpers.GetDefaultConfigPath());
                
                var chainPrefixBase58 = Base58CheckEncoding.Encode(ByteArrayHelpers.FromHexString(arg.SideChainId));
                var chainPrefix = chainPrefixBase58.Substring(0, 4);
                
                var key = keyStore.Create(arg.AccountPassword, chainPrefix);
                arg.ChainAccount = "ELF_" + chainPrefix + "_" + key.GetEncodedPublicKey();
            }

            var fileName = arg.ChainAccount + ".ak";
            var filePath = Path.Combine(ApplicationHelpers.GetDefaultConfigPath(), "keys", fileName);
            var keyContent = File.ReadAllText(filePath);

            return new Dictionary<string, string> {{fileName, keyContent}};
        }
    }
}