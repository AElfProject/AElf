using System.Collections.Generic;
using System.Linq;
using AElf.Contracts.Genesis;
using AElf.CrossChain;
using AElf.CrossChain.Grpc;
using AElf.Kernel;
using AElf.Kernel.Account.Application;
using AElf.Kernel.Consensus.AEDPoS;
using AElf.Kernel.SmartContract;
using AElf.Kernel.SmartContract.Application;
using AElf.Kernel.Token;
using AElf.Modularity;
using AElf.OS;
using AElf.OS.Network.Grpc;
using AElf.OS.Node.Application;
using AElf.OS.Node.Domain;
using AElf.OS.Rpc.ChainController;
using AElf.OS.Rpc.Net;
using AElf.OS.Rpc.Wallet;
using AElf.Runtime.CSharp;
using AElf.RuntimeSetup;
using AElf.WebApp.Web;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Volo.Abp;
using Volo.Abp.AspNetCore;
using Volo.Abp.Modularity;
using Volo.Abp.Threading;

namespace AElf.Blockchains.BasicBaseChain
{
    [DependsOn(
        typeof(AEDPoSAElfModule),
        typeof(KernelAElfModule),
        typeof(OSAElfModule),
        typeof(AbpAspNetCoreModule),
        typeof(CSharpRuntimeAElfModule),
        typeof(GrpcNetworkModule),

        //TODO: should move to OSAElfModule
        typeof(ChainControllerRpcModule),
        typeof(WalletRpcModule),
        typeof(NetRpcAElfModule),
        typeof(RuntimeSetupAElfModule),
        typeof(GrpcCrossChainAElfModule),

        //web api module
        typeof(WebWebAppAElfModule)
    )]
    public class BasicBaseChainAElfModule : AElfModule<BasicBaseChainAElfModule>
    {
        public OsBlockchainNodeContext OsBlockchainNodeContext { get; set; }

        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            var s = context.Services;
            s.TryAddSingleton<ISmartContractAddressNameProvider, ConsensusSmartContractAddressNameProvider>();
            s.TryAddSingleton<ISmartContractAddressNameProvider, CrossChainSmartContractAddressNameProvider>();
            s.TryAddSingleton<ISmartContractAddressNameProvider, ElectionSmartContractAddressNameProvider>();
            s.TryAddSingleton<ISmartContractAddressNameProvider, ParliamentAuthContractAddressNameProvider>();
            s.TryAddSingleton<ISmartContractAddressNameProvider, ProfitSmartContractAddressNameProvider>();
            s.TryAddSingleton<ISmartContractAddressNameProvider, ResourceSmartContractAddressNameProvider>();
            s.TryAddSingleton<ISmartContractAddressNameProvider, ResourceFeeReceiverSmartContractAddressNameProvider>();
            s.TryAddSingleton<ISmartContractAddressNameProvider, TokenConverterSmartContractAddressNameProvider>();
            s.TryAddSingleton<ISmartContractAddressNameProvider, TokenSmartContractAddressNameProvider>();
            s.TryAddSingleton<ISmartContractAddressNameProvider, VoteSmartContractAddressNameProvider>();

            var configuration = context.Services.GetConfiguration();
            Configure<TokenInitialOptions>(configuration.GetSection("TokenInitial"));
            Configure<ChainOptions>(option =>
            {
                option.ChainId =
                    ChainHelpers.ConvertBase58ToChainId(context.Services.GetConfiguration()["ChainId"]);
            });

            Configure<HostSmartContractBridgeContextOptions>(options =>
            {
                options.ContextVariables[ContextVariableDictionary.NativeSymbolName] = context.Services
                    .GetConfiguration().GetValue("TokenInitial:Symbol", "ELF");
            });

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

        public override void OnApplicationInitialization(ApplicationInitializationContext context)
        {
            var chainOptions = context.ServiceProvider.GetService<IOptionsSnapshot<ChainOptions>>().Value;
            var dto = new OsBlockchainNodeContextStartDto()
            {
                ChainId = chainOptions.ChainId,
                ZeroSmartContract = typeof(BasicContractZero)
            };

            var zeroContractAddress = context.ServiceProvider.GetRequiredService<ISmartContractAddressService>()
                .GetZeroSmartContractAddress();
            var dtoProvider = context.ServiceProvider.GetRequiredService<IGenesisSmartContractDtoProvider>();

            dto.InitializationSmartContracts = dtoProvider.GetGenesisSmartContractDtos(zeroContractAddress).ToList();
            dto.ContractZeroInitializationInput = dtoProvider.GetContractZeroInitializationInput();
            
            var osService = context.ServiceProvider.GetService<IOsBlockchainNodeContextService>();
            var that = this;
            AsyncHelper.RunSync(async () => { that.OsBlockchainNodeContext = await osService.StartAsync(dto); });
        }

        public override void OnApplicationShutdown(ApplicationShutdownContext context)
        {
            var osService = context.ServiceProvider.GetService<IOsBlockchainNodeContextService>();
            var that = this;
            AsyncHelper.RunSync(() => osService.StopAsync(that.OsBlockchainNodeContext));
        }
    }
}