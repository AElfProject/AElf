using System;
using System.Collections.Generic;
using System.IO;
using AElf.Common.Application;
using AElf.Cryptography;
using AElf.Management.Helper;
using AElf.Management.Models;
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
                var keyStore = new AElfKeyStore(ApplicationHelpers.GetDefaultDataDir());
                var key = keyStore.Create(arg.AccountPassword);
                arg.ChainAccount = key.GetAddressHex();
            }

            var fileName = arg.ChainAccount + ".ak";
            var filePath = Path.Combine(ApplicationHelpers.GetDefaultDataDir(), "keys", fileName);
            var keyContent = File.ReadAllText(filePath);

            return new Dictionary<string, string> {{fileName, keyContent}};
        }
    }
}