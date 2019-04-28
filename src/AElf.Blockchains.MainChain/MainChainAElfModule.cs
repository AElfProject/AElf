using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using AElf.Blockchains.BasicBaseChain;
using AElf.CrossChain;
using AElf.Kernel;
using AElf.Kernel.Consensus.DPoS;
using AElf.Kernel.Token;
using AElf.Modularity;
using AElf.OS.Node.Application;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Swashbuckle.AspNetCore.Swagger;
using Volo.Abp.Modularity;

namespace AElf.Blockchains.MainChain
{
    [DependsOn(
        typeof(BasicBaseChainAElfModule)
    )]
    public class MainChainAElfModule : AElfModule<MainChainAElfModule>
    {
        public ILogger<MainChainAElfModule> Logger { get; set; }

        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            base.ConfigureServices(context);
            context.Services.AddTransient<IGenesisSmartContractDtoProvider, GenesisSmartContractDtoProvider>();
        }

        public MainChainAElfModule()
        {
            Logger = NullLogger<MainChainAElfModule>.Instance;
        }
    }
}