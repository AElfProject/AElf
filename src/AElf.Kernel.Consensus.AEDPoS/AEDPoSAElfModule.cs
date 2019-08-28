using System.Collections.Generic;
using AElf.Kernel.Account.Application;
using AElf.Kernel.Consensus.AEDPoS.Application;
using AElf.Kernel.Consensus.Application;
using AElf.Kernel.Consensus.Scheduler.RxNet;
using AElf.Modularity;
using Google.Protobuf.WellKnownTypes;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.Modularity;
using Volo.Abp.Threading;

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
            context.Services.AddSingleton<IIrreversibleBlockDiscoveryService, IrreversibleBlockDiscoveryService>();
            context.Services.AddSingleton<IAEDPoSInformationProvider, AEDPoSInformationProvider>();
            context.Services.AddSingleton<ITriggerInformationProvider, AEDPoSTriggerInformationProvider>();
            context.Services.AddSingleton<IRandomHashCacheService, RandomHashCacheService>();
            context.Services.AddSingleton<Application.BestChainFoundEventHandler>();
            context.Services.AddSingleton<ConsensusValidationFailedEventHandler>();
            context.Services
                .AddSingleton<IBroadcastPrivilegedPubkeyListProvider, AEDPoSBroadcastPrivilegedPubkeyListProvider>();

            var configuration = context.Services.GetConfiguration();

            Configure<ConsensusOptions>(option =>
            {
                var consensusOptions = configuration.GetSection("Consensus");
                consensusOptions.Bind(option);

                var startTimeStamp = consensusOptions["StartTimestamp"];
                option.StartTimestamp = new Timestamp
                    {Seconds = string.IsNullOrEmpty(startTimeStamp) ? 0 : long.Parse(startTimeStamp)};

                if (option.InitialMinerList == null || option.InitialMinerList.Count == 0 ||
                    string.IsNullOrWhiteSpace(option.InitialMinerList[0]))
                {
                    AsyncHelper.RunSync(async () =>
                    {
                        var accountService = context.Services.GetRequiredServiceLazy<IAccountService>().Value;
                        var publicKey = (await accountService.GetPublicKeyAsync()).ToHex();
                        option.InitialMinerList = new List<string> {publicKey};
                    });
                }
            });
        }
    }
}