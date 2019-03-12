﻿using AElf.Common;
using AElf.Contracts.Consensus.DPoS;
using AElf.Contracts.Dividends;
using AElf.Contracts.Genesis;
using AElf.Contracts.Resource;
using AElf.Contracts.Resource.FeeReceiver;
using AElf.Contracts.Token;
using AElf.Kernel;
using AElf.Kernel.Consensus;
using AElf.Kernel.Consensus.DPoS;
using AElf.Kernel.SmartContract;
using AElf.Modularity;
using AElf.OS;
using AElf.OS.Network.Grpc;
using AElf.OS.Node.Application;
using AElf.OS.Node.Domain;
using AElf.OS.Rpc.ChainController;
using AElf.OS.Rpc.Net;
using AElf.OS.Rpc.Wallet;
using AElf.Runtime.CSharp;
using AElf.Runtime.CSharp.ExecutiveTokenPlugin;
using AElf.RuntimeSetup;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Volo.Abp;
using Volo.Abp.AspNetCore.Mvc;
using Volo.Abp.Autofac;
using Volo.Abp.Modularity;
using Volo.Abp.Threading;

namespace AElf.Launcher
{
    [DependsOn(
        typeof(AbpAutofacModule),
        typeof(AbpAspNetCoreMvcModule),
        typeof(RuntimeSetupAElfModule),
        typeof(DPoSConsensusAElfModule),
        typeof(KernelAElfModule),
        typeof(OSAElfModule),
        typeof(CSharpRuntimeAElfModule),
        typeof(ExecutiveTokenPluginCSharpRuntimeAElfModule),
        typeof(GrpcNetworkModule),
        
        //TODO: should move to OSAElfModule
        typeof(ChainControllerRpcModule),
        typeof(WalletRpcModule),
        typeof(NetRpcAElfModule)
    )]
    public class LauncherAElfModule : AElfModule
    {
        public static IConfigurationRoot Configuration;

        public ILogger<LauncherAElfModule> Logger { get; set; }

        public OsBlockchainNodeContext OsBlockchainNodeContext { get; set; }

        public LauncherAElfModule()
        {
            Logger = NullLogger<LauncherAElfModule>.Instance;
        }

        public override void PreConfigureServices(ServiceConfigurationContext context)
        {
            context.Services.SetConfiguration(Configuration);

            Configure<ChainOptions>(option => option.ChainId = ChainHelpers.ConvertBase58ToChainId(Configuration["ChainId"]));
        }

        public override void ConfigureServices(ServiceConfigurationContext context)
        {
        }

        public override void OnPreApplicationInitialization(ApplicationInitializationContext context)
        {
            
        }

        public override void OnApplicationInitialization(ApplicationInitializationContext context)
        {
            var chainOptions = context.ServiceProvider.GetService<IOptionsSnapshot<ChainOptions>>().Value;
            var dto = new OsBlockchainNodeContextStartDto()
            {
                ChainId = chainOptions.ChainId,
                ZeroSmartContract = typeof(BasicContractZero)
            };

            dto.InitializationSmartContracts.AddGenesisSmartContract<ConsensusContract>(ConsensusSmartContractAddressNameProvider.Name);
            dto.InitializationSmartContracts.AddGenesisSmartContract<TokenContract>(TokenSmartContractAddressNameProvider.Name);
            dto.InitializationSmartContracts.AddGenesisSmartContract<DividendsContract>(DividendsSmartContractAddressNameProvider.Name);
            dto.InitializationSmartContracts.AddGenesisSmartContract<ResourceContract>(ResourceSmartContractAddressNameProvider.Name);
            dto.InitializationSmartContracts.AddGenesisSmartContract<FeeReceiverContract>(ResourceFeeReceiverSmartContractAddressNameProvider.Name);

            var osService = context.ServiceProvider.GetService<IOsBlockchainNodeContextService>();
            var that = this;
            AsyncHelper.RunSync(async () => { that.OsBlockchainNodeContext = await osService.StartAsync(dto); });
        }

        public override void OnPostApplicationInitialization(ApplicationInitializationContext context)
        {
        }

        public override void OnApplicationShutdown(ApplicationShutdownContext context)
        {
            var osService = context.ServiceProvider.GetService<IOsBlockchainNodeContextService>();
            var that = this;
            AsyncHelper.RunSync(() => osService.StopAsync(that.OsBlockchainNodeContext));
        }
    }
}