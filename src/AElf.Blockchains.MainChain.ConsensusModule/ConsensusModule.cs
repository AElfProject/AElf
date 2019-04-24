using System.Collections.Generic;
using AElf.Kernel.Account.Application;
using AElf.Modularity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.Modularity;
using Volo.Abp.Threading;

namespace AElf.Blockchains.MainChain.ConsensusModule
{
    public class ConsensusModule : AElfModule<ConsensusModule>
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            var configuration = context.Services.GetConfiguration();

            Configure<DPoSOptions>(option =>
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