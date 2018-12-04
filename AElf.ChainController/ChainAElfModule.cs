using System.IO;
using System.Linq;
using AElf.Common.Application;
using AElf.Common;
using AElf.Common.Module;
using AElf.Configuration;
using AElf.Configuration.Config.Chain;
using Autofac;
using Easy.MessageHub;
using Google.Protobuf;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace AElf.ChainController
{
    public class ChainAElfModule:IAElfModule
    {
        private static readonly string FileFolder = Path.Combine(ApplicationHelpers.ConfigPath, "config");
        private static readonly string FilePath = Path.Combine(FileFolder, @"chain.json");
        
        public void Init(ContainerBuilder builder)
        {
            if (NodeConfig.Instance.IsChainCreator)
            {
                string chainId;
                if (string.IsNullOrWhiteSpace(ChainConfig.Instance.ChainId))
                {
                    chainId = Hash.Generate().DumpHex();
                    ChainConfig.Instance.ChainId = chainId;
                }
                else
                {
                    chainId = ChainConfig.Instance.ChainId;
                }

                var obj = new JObject(new JProperty("ChainId", chainId));

                // write JSON directly to a file
                if (!Directory.Exists(FileFolder))
                {
                    Directory.CreateDirectory(FileFolder);
                }

                using (var file = File.CreateText(FilePath))
                using (var writer = new JsonTextWriter(file))
                {
                    obj.WriteTo(writer);
                }
            }

            builder.RegisterModule(new ChainAutofacModule());
           
        }

        public void Run(ILifetimeScope scope)
        {
        }
    }
}