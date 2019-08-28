using System;
using System.Linq;
using AElf.Blockchains.BasicBaseChain;
using AElf.Kernel;
using AElf.Modularity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Volo.Abp.Modularity;

namespace AElf.Blockchains.MainChain
{
    [DependsOn(
        typeof(BasicBaseChainAElfModule),
        typeof(BlockTransactionLimitControllerModule)
    )]
    public class MainChainAElfModule : AElfModule
    {
        public ILogger<MainChainAElfModule> Logger { get; set; }

        public MainChainAElfModule()
        {
            Logger = NullLogger<MainChainAElfModule>.Instance;
        }
        
        public override void PostConfigureServices(ServiceConfigurationContext context)
        {
            base.PostConfigureServices(context);
            var res = context.Services.GroupBy(s => s.ServiceType)
                .Select(g => new {ServiceType = g.Key, Count = g.Count()})
                .Where(r => r.Count > 1)
                .OrderBy(x => x.ServiceType.ToString())
                .ToList();
            foreach (var re in res)
            {
                Console.WriteLine("ServiceType:{0}, Amount:{1}.", re.ServiceType, re.Count);
            }
        }
    }
}