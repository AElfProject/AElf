using System.IO;
using AElf.Common.Application;
using AElf.Common;
using AElf.Common.Module;
using AElf.Configuration;
using Autofac;
using Easy.MessageHub;
using Google.Protobuf;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace AElf.ChainController
{
    public class ChainAElfModule:IAElfModule
    {
        private static readonly string FileFolder = Path.Combine(ApplicationHelpers.GetDefaultDataDir(), "chain");
        private static readonly string FilePath = Path.Combine(FileFolder, @"ChainInfo.json");
        
        public void Init(ContainerBuilder builder)
        {
            Hash chainIdHash;
            if (NodeConfig.Instance.IsChainCreator)
            {
                if (string.IsNullOrWhiteSpace(NodeConfig.Instance.ChainId))
                {
                    chainIdHash = Hash.Generate();
                }
                else
                {
                    chainIdHash = Hash.LoadHex(NodeConfig.Instance.ChainId);
                }

                var obj = new JObject(new JProperty("id", chainIdHash.DumpHex()));

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
            else
            {
                // read JSON directly from a file
                using (var file = File.OpenText(FilePath))
                using (var reader = new JsonTextReader(file))
                {
                    var chain = (JObject) JToken.ReadFrom(reader);
                    chainIdHash = Hash.LoadHex(chain.GetValue("id").ToString());
                }
            }

            NodeConfig.Instance.ChainId = chainIdHash.DumpHex();

            builder.RegisterModule(new ChainAutofacModule());
           
        }

        public void Run(ILifetimeScope scope)
        {
        }
    }
}