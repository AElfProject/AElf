using System.Threading.Tasks;
using AElf.Contracts.Consensus.AEDPoS;
using AElf.CSharp.Core.Extension;
using AElf.Kernel;
using AElf.Kernel.Blockchain.Application;
using AElf.Kernel.FeeCalculation;
using AElf.Kernel.SmartContract.Application;
using AElf.Kernel.SmartContract.ExecutionPluginForMethodFee;
using AElf.Kernel.TransactionPool;
using AElf.Modularity;
using AElf.OS;
using AElf.OS.Network.Application;
using AElf.OS.Network.Infrastructure;
using AElf.WebApp.Web;
using Google.Protobuf;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Moq;
using Volo.Abp.AspNetCore.TestBase;
using Volo.Abp.Autofac;
using Volo.Abp.Modularity;

namespace AElf.WebApp.Application;

[DependsOn(
    typeof(AbpAutofacModule),
    typeof(AbpAspNetCoreTestBaseModule),
    typeof(WebWebAppAElfModule),
    typeof(OSCoreWithChainTestAElfModule),
    typeof(FeeCalculationModule)
)]
public class WebAppTestAElfModule : AElfModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        context.Services.Replace(ServiceDescriptor.Singleton<INetworkService, NetworkService>());

        context.Services.Replace(ServiceDescriptor.Singleton(o =>
        {
            var pool = o.GetService<IPeerPool>();
            var serverMock = new Mock<IAElfNetworkServer>();

            serverMock.Setup(p => p.DisconnectAsync(It.IsAny<IPeer>(), It.IsAny<bool>()))
                .Returns(Task.CompletedTask)
                .Callback<IPeer, bool>((peer, disc) => pool.RemovePeer(peer.Info.Pubkey));

            return serverMock.Object;
        }));

        context.Services.AddSingleton(provider =>
        {
            var mockService = new Mock<IBlockExtraDataService>();
            mockService.Setup(m => m.GetExtraDataFromBlockHeader(It.IsAny<string>(), It.IsAny<BlockHeader>()))
                .Returns(ByteString.CopyFrom(new AElfConsensusHeaderInformation
                {
                    Behaviour = AElfConsensusBehaviour.NextRound,
                    Round = new Round
                    {
                        RoundNumber = 12,
                        TermNumber = 1,
                        BlockchainAge = 3,
                        ExtraBlockProducerOfPreviousRound = "bp2-pubkey",
                        MainChainMinersRoundNumber = 3,
                        RealTimeMinersInformation =
                        {
                            {
                                "bp1-pubkey", new MinerInRound
                                {
                                    Order = 2,
                                    ProducedBlocks = 3,
                                    ExpectedMiningTime = TimestampHelper.GetUtcNow().AddSeconds(3),
                                    MissedTimeSlots = 1
                                }
                            }
                        }
                    },
                    SenderPubkey = ByteString.CopyFromUtf8("pubkey")
                }.ToByteArray()));

            return mockService.Object;
        });

        context.Services.AddSingleton<IPreExecutionPlugin, FeeChargePreExecutionPlugin>();
        context.Services.AddTransient<ITransactionSizeFeeSymbolsProvider, TransactionSizeFeeSymbolsProvider>();
        context.Services.Replace(ServiceDescriptor
            .Singleton<ITransactionExecutingService, PlainTransactionExecutingService>());
        Configure<BasicAuthOptions>(options =>
        {
            options.UserName = BasicAuth.DefaultUserName;
            options.Password = BasicAuth.DefaultPassword;
        });
        Configure<TransactionOptions>(o => { 
            o.PoolLimit = 20;
            o.StoreInvalidTransactionResultEnabled = true;
        });
    }
}