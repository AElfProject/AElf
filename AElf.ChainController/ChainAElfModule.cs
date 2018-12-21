using System.IO;
using AElf.ChainController.CrossChain;
using AElf.Common.Application;
using AElf.Common;
using AElf.Configuration;
using AElf.Configuration.Config.Chain;
using AElf.Modularity;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Volo.Abp;
using Volo.Abp.Modularity;

namespace AElf.ChainController
{
    public class ChainAElfModule : AElfModule
    {
        private static readonly string FileFolder = Path.Combine(ApplicationHelpers.ConfigPath, "config");
        private static readonly string FilePath = Path.Combine(FileFolder, @"chain.json");


        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            var services = context.Services;
            services.AddAssemblyOf<ChainAElfModule>();


            services.AddSingleton<ICrossChainInfoReader, CrossChainInfoReader>();
            /*builder.RegisterAssemblyTypes(assembly).AsImplementedInterfaces();
            builder.RegisterType<ChainCreationService>().As<IChainCreationService>();
            builder.RegisterType<ChainContextService>().As<IChainContextService>();
            builder.RegisterType<ChainService>().As<IChainService>().SingleInstance();
            builder.RegisterType<CrossChainInfoReader>().As<ICrossChainInfoReader>().SingleInstance();*/
        }

        public override void OnApplicationInitialization(ApplicationInitializationContext context)
        {
            if (NodeConfig.Instance.IsChainCreator)
            {
                string chainId;
                if (string.IsNullOrWhiteSpace(ChainConfig.Instance.ChainId))
                {
                    chainId = GlobalConfig.DefaultChainId;
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
        }
    }
}