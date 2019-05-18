using System.Collections.Generic;
using AElf.Kernel.Account.Application;
using AElf.Kernel.Blockchain.Application;
using AElf.Kernel.Consensus.AEDPoS.Application;
using AElf.Kernel.Consensus.Application;
using AElf.Kernel.Consensus.Infrastructure;
using AElf.Kernel.Consensus.Scheduler.RxNet;
using AElf.Kernel.Miner.Application;
using AElf.Modularity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.Modularity;
using Volo.Abp.Threading;
using BestChainFoundEventHandler = AElf.Kernel.Consensus.Application.BestChainFoundEventHandler;

namespace AElf.Kernel.Consensus.AEDPoS
{
    [DependsOn(
        typeof(RxNetSchedulerAElfModule),
        typeof(ConsensusAElfModule)
    )]
    // ReSharper disable once InconsistentNaming
    public class AEDPoSAElfModule : AElfModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            context.Services.AddAssemblyOf<AEDPoSAElfModule>();

            context.Services.AddSingleton<IIrreversibleBlockDiscoveryService, IrreversibleBlockDiscoveryService>();
            context.Services.AddSingleton<IAEDPoSInformationProvider, AEDPoSInformationProvider>();
            context.Services.AddSingleton<ITriggerInformationProvider, AEDPoSTriggerInformationProvider>();
            context.Services.AddSingleton<BestChainFoundEventHandler>();

            var configuration = context.Services.GetConfiguration();

            Configure<ConsensusOptions>(option =>
            {
                configuration.GetSection("Consensus").Bind(option);

                if (option.InitialMiners == null || option.InitialMiners.Count == 0 ||
                    string.IsNullOrWhiteSpace(option.InitialMiners[0]))
                {
                    AsyncHelper.RunSync(async () =>
                    {
                        var accountService = context.Services.GetRequiredServiceLazy<IAccountService>().Value;
                        var publicKey = (await accountService.GetPublicKeyAsync()).ToHex();
                        option.InitialMiners = new List<string> {publicKey};
                    });
                }
            });
        }
    }
}