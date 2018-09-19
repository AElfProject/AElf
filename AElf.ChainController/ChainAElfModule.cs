using System.IO;
using AElf.ChainController.EventMessages;
using AElf.ChainController.TxMemPool;
using AElf.Common.Application;
using AElf.Common.ByteArrayHelpers;
using AElf.Common.Module;
using AElf.Configuration;
using AElf.Kernel;
using Autofac;
using Easy.MessageHub;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace AElf.ChainController
{
    public class ChainAElfModule:IAElfModule
    {
        private static readonly string FilePath =
            Path.Combine(ApplicationHelpers.GetDefaultDataDir(), "chain", @"ChainInfo.json");
        
        public void Init(ContainerBuilder builder)
        {
            Hash chainIdHash;
            if (NodeConfig.Instance.IsChainCreator)
            {
                if (string.IsNullOrWhiteSpace(NodeConfig.Instance.ChainId))
                {
                    chainIdHash = Hash.Generate().ToChainId();
                }
                else
                {
                    chainIdHash = ByteArrayHelpers.FromHexString(NodeConfig.Instance.ChainId);
                }

                var obj = new JObject(new JProperty("id", chainIdHash.ToHex()));

                // write JSON directly to a file
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
                    chainIdHash = ByteArrayHelpers.FromHexString(chain.GetValue("id").ToString());
                }
            }

            NodeConfig.Instance.ChainId = chainIdHash.ToHex();

            builder.RegisterModule(new ChainAutofacModule());

            var txPoolConfig = TxPoolConfig.Default;
            txPoolConfig.FeeThreshold = 0;
            txPoolConfig.PoolLimitSize = TransactionPoolConfig.Instance.PoolLimitSize;
            txPoolConfig.Maximal = TransactionPoolConfig.Instance.Maximal;
            txPoolConfig.EcKeyPair = TransactionPoolConfig.Instance.EcKeyPair;
            txPoolConfig.ChainId = chainIdHash;
            builder.RegisterInstance(txPoolConfig).As<ITxPoolConfig>();
        }

        public void Run(ILifetimeScope scope)
        {
        }
    }
}