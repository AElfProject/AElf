using System.Threading.Tasks;
using Acs7;
using AElf.CrossChain.Application;
using AElf.CrossChain.Indexing.Application;
using AElf.CrossChain.Indexing.Infrastructure;
using AElf.Kernel;
using AElf.Kernel.SmartContract;
using AElf.Modularity;
using AElf.Types;
using Google.Protobuf;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Volo.Abp.Modularity;

namespace AElf.CrossChain
{
    [DependsOn(typeof(CoreKernelAElfModule),
        typeof(CrossChainModule),
        typeof(KernelCoreTestAElfModule),
        typeof(SmartContractAElfModule)
    )]
    public class CrossChainTestModule : AElfModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            context.Services.AddSingleton<CrossChainTestHelper>();
            context.Services.AddTransient(provider =>
            {
                var mockCrossChainRequestService = new Mock<ICrossChainRequestService>();
                mockCrossChainRequestService.Setup(mock => mock.RequestCrossChainDataFromOtherChainsAsync())
                    .Returns(Task.CompletedTask);
                return mockCrossChainRequestService.Object;
            });
            
            context.Services.AddTransient(provider =>
            {
                var mockCrossChainIndexingDataService = new Mock<ICrossChainIndexingDataService>();
                mockCrossChainIndexingDataService
                    .Setup(m => m.GetIndexedCrossChainBlockDataAsync(It.IsAny<Hash>(), It.IsAny<long>()))
                    .Returns<Hash, long>((blockHash, blockHeight) =>
                    {
                        var crossChainTestHelper =
                            context.Services.GetRequiredServiceLazy<CrossChainTestHelper>().Value;

                        return Task.FromResult(crossChainTestHelper.GetIndexedCrossChainExtraData(blockHeight));
                    });

                mockCrossChainIndexingDataService.Setup(m =>
                        m.GetCrossChainTransactionInputForNextMiningAsync(It.IsAny<Hash>(), It.IsAny<long>()))
                    .Returns<Hash, long>(
                        (previousHash, height) =>
                        {
                            var crossChainTestHelper =
                                context.Services.GetRequiredServiceLazy<CrossChainTestHelper>().Value;
                            return Task.FromResult(crossChainTestHelper.GetCrossChainBlockData(previousHash));
                        });

                mockCrossChainIndexingDataService.Setup(m =>
                        m.PrepareExtraDataForNextMiningAsync(It.IsAny<Hash>(), It.IsAny<long>()))
                    .Returns<Hash, long>(
                        (previousHash, height) =>
                        {
                            var crossChainTestHelper =
                                context.Services.GetRequiredServiceLazy<CrossChainTestHelper>().Value;
                            return Task.FromResult(
                                crossChainTestHelper.GetCrossChainExtraData(previousHash)?.ToByteString() ??
                                ByteString.Empty);
                        });

                mockCrossChainIndexingDataService.Setup(m =>
                    m.GetAllChainIdHeightPairsAtLibAsync()).Returns(() =>
                {
                    var crossChainTestHelper =
                        context.Services.GetRequiredServiceLazy<CrossChainTestHelper>().Value;
                    return Task.FromResult(crossChainTestHelper.GetAllIndexedCrossChainExtraData());
                });
                return mockCrossChainIndexingDataService.Object;
            });
        }
    }
}