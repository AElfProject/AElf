﻿using AElf.Blockchains.BasicBaseChain;
using AElf.Kernel.SmartContract.Application;
using AElf.Database;
using AElf.Kernel.Consensus.AEPoW;
using AElf.Kernel.Infrastructure;
using AElf.Modularity;
using AElf.OS.Node.Application;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Volo.Abp.Modularity;

namespace AElf.Blockchains.PoWChain
{
    [DependsOn(
        typeof(BasicBaseChainAElfModule),
        typeof(AEPoWAElfModule)
    )]
    public class PoWChainAElfModule : AElfModule
    {
        public ILogger<PoWChainAElfModule> Logger { get; set; }

        public PoWChainAElfModule()
        {
            Logger = NullLogger<PoWChainAElfModule>.Instance;
        }

        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            var services = context.Services;
            services.AddTransient<IContractDeploymentListProvider, PoWChainContractDeploymentListProvider>();
            services.AddTransient<IGenesisSmartContractDtoProvider, PoWChainGenesisSmartContractDtoProvider>();

            var config = context.Services.GetConfiguration();

            if (config.GetConnectionString("BlockchainDb") == "InMemory")
            {
                services.AddKeyValueDbContext<BlockchainKeyValueDbContext>(p => p.UseInMemoryDatabase());
            }

            if (config.GetConnectionString("StateDb") == "InMemory")
            {
                services.AddKeyValueDbContext<StateKeyValueDbContext>(p => p.UseInMemoryDatabase());
            }
        }
    }
}