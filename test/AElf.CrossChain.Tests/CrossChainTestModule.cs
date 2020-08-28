using System.Threading.Tasks;
using AElf.Standards.ACS7;
using AElf.CrossChain.Application;
using AElf.CrossChain.Indexing.Application;
using AElf.Kernel;
using AElf.Kernel.SmartContract;
using AElf.Kernel.SmartContract.Application;
using AElf.Kernel.Txn.Application;
using AElf.Modularity;
using AElf.Types;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using NSubstitute;
using Volo.Abp.Modularity;

namespace AElf.CrossChain
{
    [DependsOn(
        typeof(CrossChainAElfModule),
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
                    .Setup(m => m.GetIndexedSideChainBlockDataAsync(It.IsAny<Hash>(), It.IsAny<long>()))
                    .Returns<Hash, long>((blockHash, blockHeight) =>
                    {
                        var crossChainTestHelper =
                            context.Services.GetRequiredServiceLazy<CrossChainTestHelper>().Value;
                        var crossChainBlockData = crossChainTestHelper.GetIndexedCrossChainExtraData(blockHeight);
                        var indexedSideChainBlockData = new IndexedSideChainBlockData
                        {
                            SideChainBlockDataList = {crossChainBlockData.SideChainBlockDataList},
                        };
                        return Task.FromResult(indexedSideChainBlockData);
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

                mockCrossChainIndexingDataService
                    .Setup(
                        m => m.CheckExtraDataIsNeededAsync(It.IsAny<Hash>(), It.IsAny<long>(), It.IsAny<Timestamp>()))
                    .Returns<Hash, long, Timestamp>((blockHash, height, timeStamp) =>
                    {
                        var crossChainTestHelper =
                            context.Services.GetRequiredServiceLazy<CrossChainTestHelper>().Value;
                        return Task.FromResult(crossChainTestHelper.GetCrossChainExtraData(blockHash) != null);
                    });
                return mockCrossChainIndexingDataService.Object;
            });

            context.Services.AddTransient(provider =>
            {
                var mockService = new Mock<ISmartContractAddressService>();
                mockService.Setup(m => m.GetAddressByContractNameAsync(It.IsAny<IChainContext>(),
                        It.IsAny<string>()))
                    .Returns(Task.FromResult(default(Address)));
                return mockService.Object;
            });

            context.Services.AddSingleton<ITransactionPackingOptionProvider, MockTransactionPackingOptionProvider>();
        }
    }
}