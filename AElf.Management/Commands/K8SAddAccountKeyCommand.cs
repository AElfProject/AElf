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

        private Dictionary<string, string> GetAccoutKey(string chainId, DeployArg arg)
        {
            //todo get from main chain namespace
            var keyContent1 = "-----BEGIN EC PRIVATE KEY-----"
                             + Environment.NewLine + "Proc-Type: 4,ENCRYPTED"
                             + Environment.NewLine + "DEK-Info: AES-256-CFB,32076f2bc8b3f51b4fe2a80bb7759e39"
                             + Environment.NewLine + "iQiOsaDjSI64m+zSHGZyEXvphJnfYLhL+pzJ0Lup+CeoRmod9ODpd9bqaO1kvGx4"
                             + Environment.NewLine + "jPR1si2QiKEAukVdZrvBtEnY6NQWwtobG+Zq2UWCxaIER8/prb7UGSKebdL6SC7M"
                             + Environment.NewLine + "nxSan8PEZ8aFG/koKslhXweGcrSDYV9RdZkdQ39sUZp5fcFa8N+JhDY20CLrgtIX"
                             + Environment.NewLine + "2Xm3xb2Ew2Mh+RNoXpzn4iz2wB93XMQjOBRW4UqvTJOgCkNdQuwIq6rYLBK1sd6I"
                             + Environment.NewLine + "owxBGoiUbTCuw1PcUCf8zcO0VzzIcsqLqLMIqwDKKjYYafVZU7xMp0i58gIoWl1d"
                             + Environment.NewLine + "JUImoT0JiEbBEv354hqj3pb+5MzHUX37B5l3iGuFZA=="
                             + Environment.NewLine + "-----END EC PRIVATE KEY-----";
            var keyContent2 = "-----BEGIN EC PRIVATE KEY-----"
                              + Environment.NewLine + "Proc-Type: 4,ENCRYPTED"
                              + Environment.NewLine + "DEK-Info: AES-256-CFB,7a7ff9d5acacbc11aa4dd064331e714e"
                              + Environment.NewLine + "cHkG33K7ywsoaVUPbq5FWIbHhESD1qXgUVuiVo8K0honG9EKwt2QALwpkP0PqKc4"
                              + Environment.NewLine + "uFDaxqcjeU/dfFMlDWQYavHEw6iHNRKc78Xs8YdaTR2xubNGIkKW/3klxBFygDgR"
                              + Environment.NewLine + "3r+KEtpAWoKj5O+sovdd+DIY1Fu+zGDoNDIg7A5yX2d/eRkDXrOT5ux9bO1JTZ5k"
                              + Environment.NewLine + "22SuqOnjrbqHL1Cx0Yi8xPNjpN5fh2KfMneJ5DPvj6ToL0b624I3sMLtAV71MOar"
                              + Environment.NewLine + "qIFnUtnO0F9pULdGlymTuZNsTT8kzt1jDgowcUz1Kgay6BSzWuXojPhMO6Xii3lS"
                              + Environment.NewLine + "vptUH1lF3o3UIEsIjDIcHCTU6ndFT0Hk+v/Ke6y1lQ=="
                              + Environment.NewLine + "-----END EC PRIVATE KEY-----";
            var keyContent3 = "-----BEGIN EC PRIVATE KEY-----"
                              + Environment.NewLine + "Proc-Type: 4,ENCRYPTED"
                              + Environment.NewLine + "DEK-Info: AES-256-CFB,0f1ec027d26420f96d14e6cd88404912"
                              + Environment.NewLine + "XlfPImHb2Qmyqso/tuu90kfHkRYvy56M/AgG+OLVQbidiM4JtF536J0fsZ6YU7SC"
                              + Environment.NewLine + "ah1hENnbj7k0UlTInqKe90NLniSZ7PcjDXtdvTN1N9/fnvX/WUnZqW5jcYX4+O7/"
                              + Environment.NewLine + "E2k/2HL+hJhbS5skp8v/82LfPbweCHypNiqz45g4WdBlnvo9kFShiDJsI/FMiPjV"
                              + Environment.NewLine + "gOHC8stXmnqDWAM3MUfihtAW7Xn/kBOxZF0SG/2Ml6Hy4PR+aYK7yq6iZppawig+"
                              + Environment.NewLine + "8726SCllYbRgn3an/xx3Pyy5FBjmS+tFN0qtzwIChgVQ1vK2Jo2NT9m48Galeanr"
                              + Environment.NewLine + "07nQ3EPdV9Vx++lvpFA1YKp9FMTdkhuNy0nVbVvSmA=="
                              + Environment.NewLine + "-----END EC PRIVATE KEY-----";
            
            var result = new Dictionary<string, string>
            {
                { "0x04b8b111fdbc2f5409a006339fa1758e1ed1.ak",keyContent1},
                { "0x0429c477d551aa91abc193d7088f69082000.ak",keyContent2},
                { "0x04bce3e67ec4fbd0fad2822e6e5ed097812c.ak",keyContent3}
            };

            return result;
        }
    }
}