using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using AElf.Common;
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
        public async Task Action(DeployArg arg)
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
                Data = await GetAndCreateAccountKey(arg)
            };

            await K8SRequestHelper.GetClient().CreateNamespacedConfigMapAsync(body, arg.SideChainId);
        }

        private async Task<Dictionary<string, string>> GetAndCreateAccountKey(DeployArg arg)
        {
            if (string.IsNullOrWhiteSpace(arg.ChainAccount))
            {
                var keyStore = new AElfKeyStore(ApplicationHelper.AppDataPath);

                var chainPrefixBase58 = Base58CheckEncoding.Encode(ByteArrayHelpers.FromHexString(arg.SideChainId));
                var chainPrefix = chainPrefixBase58.Substring(0, 4);

                var key = await keyStore.CreateAsync(arg.AccountPassword, chainPrefix);
                // todo clean
                //arg.ChainAccount = "ELF_" + chainPrefix + "_" + key.GetEncodedPublicKey();
            }

            var fileName = arg.ChainAccount + ".ak";
            var filePath = Path.Combine(ApplicationHelper.AppDataPath, "keys", fileName);
            var keyContent = File.ReadAllText(filePath);

            return new Dictionary<string, string> {{fileName, keyContent}};
        }
    }
}