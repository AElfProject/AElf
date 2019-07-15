using System.Collections.Generic;
using System.Linq;
using AElf.Contracts.Deployer;
using AElf.Contracts.Genesis;
using AElf.Kernel;
using AElf.Kernel.Consensus;
using AElf.Kernel.Consensus.AEDPoS;
using AElf.Kernel.Token;
using AElf.Modularity;
using AElf.OS;
using AElf.OS.Network.Grpc;
using AElf.OS.Node.Application;
using AElf.OS.Node.Domain;
using AElf.OS.Rpc.ChainController;
using AElf.OS.Rpc.Net;
using AElf.Runtime.CSharp;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Volo.Abp;
using Volo.Abp.Autofac;
using Volo.Abp.Modularity;
using Volo.Abp.Threading;

namespace AElf.TestLauncher
{
    [DependsOn(
        typeof(AbpAutofacModule),
        //typeof(AbpAspNetCoreMvcModule),
        //typeof(RuntimeSetupAElfModule),
        typeof(AEDPoSAElfModule),
        typeof(KernelAElfModule),
        typeof(OSAElfModule),
        typeof(CSharpRuntimeAElfModule),
        typeof(GrpcNetworkModule),

        typeof(ChainControllerRpcModule),
        typeof(NetRpcAElfModule)
    )]
    public class MainBlockchainAElfModule : AElfModule
    {
        private IReadOnlyDictionary<string, byte[]> _codes;

        public IReadOnlyDictionary<string, byte[]> Codes =>
            _codes ?? (_codes = ContractsDeployer.GetContractCodes<MainBlockchainAElfModule>());
        public byte[] ConsensusContractCode =>
            Codes.Single(kv => kv.Key.Split(",").First().Trim().EndsWith("Consensus.AEDPoS")).Value;
        public byte[] ElectionContractCode =>
            Codes.Single(kv => kv.Key.Split(",").First().Trim().EndsWith("Election")).Value;
        public byte[] TokenContractCode =>
            Codes.Single(kv => kv.Key.Split(",").First().Trim().EndsWith("MultiToken")).Value;
        public byte[] FeeReceiverContractCode =>
            Codes.Single(kv => kv.Key.Split(",").First().Trim().EndsWith("FeeReceiver")).Value;
        public ILogger<MainBlockchainAElfModule> Logger { get; set; }

        public OsBlockchainNodeContext OsBlockchainNodeContext { get; set; }

        public MainBlockchainAElfModule()
        {
            Logger = NullLogger<MainBlockchainAElfModule>.Instance;
        }

        public override void PreConfigureServices(ServiceConfigurationContext context)
        {
        }

        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            var config = context.Services.GetConfiguration();
            Configure<ChainOptions>(option => option.ChainId = ChainHelper.ConvertBase58ToChainId(config["ChainId"]));
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

            dto.InitializationSmartContracts.AddGenesisSmartContract(
                ConsensusContractCode,
                ConsensusSmartContractAddressNameProvider.Name);
            dto.InitializationSmartContracts.AddGenesisSmartContract(
                TokenContractCode,
                TokenSmartContractAddressNameProvider.Name);
            dto.InitializationSmartContracts.AddGenesisSmartContract(
                ElectionContractCode,
                ElectionSmartContractAddressNameProvider.Name);
            dto.InitializationSmartContracts.AddGenesisSmartContract(
                FeeReceiverContractCode,
                ResourceFeeReceiverSmartContractAddressNameProvider.Name);

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