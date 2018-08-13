using System.Collections.Generic;
using AElf.Deployment.Helper;
using AElf.Deployment.Models;
using k8s.Models;
using System;

namespace AElf.Deployment.Command
{
    public class K8SAddAccountKeyCommand : IDeployCommand
    {
        private const string ConfigName = "config-keys";

        public void Action(string chainId, DeployArg arg)
        {
            var body = new V1ConfigMap
            {
                ApiVersion = V1ConfigMap.KubeApiVersion,
                Kind = V1ConfigMap.KubeKind,
                Metadata = new V1ObjectMeta
                {
                    Name = ConfigName,
                    NamespaceProperty = chainId
                },
                Data = GetAccoutKey(chainId,arg)
            };

            K8SRequestHelper.CreateNamespacedConfigMap(body, chainId);
        }

        private Dictionary<string, string> GetAccoutKey(string chainId, DeployArg arg)
        {
            //todo get from main chain namespace
            var keyContent = "-----BEGIN EC PRIVATE KEY-----"
                             + Environment.NewLine + "Proc-Type: 4,ENCRYPTED"
                             + Environment.NewLine + "DEK-Info: AES-256-CFB,32076f2bc8b3f51b4fe2a80bb7759e39"
                             + Environment.NewLine + "iQiOsaDjSI64m+zSHGZyEXvphJnfYLhL+pzJ0Lup+CeoRmod9ODpd9bqaO1kvGx4"
                             + Environment.NewLine + "jPR1si2QiKEAukVdZrvBtEnY6NQWwtobG+Zq2UWCxaIER8/prb7UGSKebdL6SC7M"
                             + Environment.NewLine + "nxSan8PEZ8aFG/koKslhXweGcrSDYV9RdZkdQ39sUZp5fcFa8N+JhDY20CLrgtIX"
                             + Environment.NewLine + "2Xm3xb2Ew2Mh+RNoXpzn4iz2wB93XMQjOBRW4UqvTJOgCkNdQuwIq6rYLBK1sd6I"
                             + Environment.NewLine + "owxBGoiUbTCuw1PcUCf8zcO0VzzIcsqLqLMIqwDKKjYYafVZU7xMp0i58gIoWl1d"
                             + Environment.NewLine + "JUImoT0JiEbBEv354hqj3pb+5MzHUX37B5l3iGuFZA=="
                             + Environment.NewLine + "-----END EC PRIVATE KEY-----";
            var result = new Dictionary<string, string> {{ arg.MainChainAccount + ".ak",keyContent}};

            return result;
        }
    }
}