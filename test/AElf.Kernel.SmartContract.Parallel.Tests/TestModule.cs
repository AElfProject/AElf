using System.IO;
using System.Threading.Tasks;
using AElf.Kernel.Blockchain.Application;
using AElf.Kernel.SmartContract.Application;
using AElf.Kernel.SmartContract.Infrastructure;
using AElf.Kernel.SmartContract.Parallel.Domain;
using AElf.Runtime.CSharp;
using AElf.Types;
using Google.Protobuf;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Volo.Abp.Modularity;
using Volo.Abp.Threading;
using ServiceDescriptor = Google.Protobuf.Reflection.ServiceDescriptor;

namespace AElf.Kernel.SmartContract.Parallel.Tests
{
    public class TestModule : AbpModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            var services = context.Services;
            services.AddSingleton<IResourceExtractionService, ResourceExtractionService>();
            var executiveService = GetSmartContractExecutiveService(
                (InternalConstants.NonAcs2, GetNonAcs2Executive()),
                (InternalConstants.Acs2, GetAcs2Executive())
            );
            services.AddSingleton(executiveService);
            context.Services.AddSingleton<IBlockchainService>(
                _ =>
                {
                    var mock = new Mock<IBlockchainService>();
                    mock.Setup(s => s.GetChainAsync()).Returns(Task.FromResult<Chain>(new Chain()
                    {
                        BestChainHash = Hash.Empty
                    }));
                    return mock.Object;
                });
            context.Services.AddSingleton<ICodeRemarksManager, MockCodeRemarksManager>();
        }

        #region Mocks

        #region NonAcs2

        private static IExecutive GetNonAcs2Executive()
        {
            var emptyServiceDescriptors = new ServiceDescriptor[0];
            var executive = new Mock<IExecutive>();
            executive.SetupGet(e => e.Descriptors).Returns(emptyServiceDescriptors);
            return executive.Object;
        }

        #endregion

        #region Acs2

        private static IExecutive GetAcs2Executive()
        {
            var testContractFile = typeof(SmartContractExecution.Parallel.Tests.TestContract.TestContract).Assembly
                .Location;
            var code = File.ReadAllBytes(testContractFile);
            var runner = new SmartContractRunnerForCategoryZero(
                Path.GetDirectoryName(testContractFile)
            );
            var executive = AsyncHelper.RunSync(() => runner.RunAsync(new SmartContractRegistration()
            {
                Category = 0,
                Code = ByteString.CopyFrom(code),
                CodeHash = Hash.FromRawBytes(code)
            }));
            executive.SetHostSmartContractBridgeContext(Mock.Of<IHostSmartContractBridgeContext>());
            return executive;
        }

        #endregion

        private static ISmartContractExecutiveService GetSmartContractExecutiveService(
            params (string, IExecutive)[] named)
        {
            var executiveService = new Mock<ISmartContractExecutiveService>();
            foreach (var tuple in named)
            {
                executiveService.Setup(
                    s => s.GetExecutiveAsync(It.IsAny<IChainContext>(),
                        It.Is<Address>(address => address == AddressHelper.Base58StringToAddress(tuple.Item1)))
                ).Returns(Task.FromResult(tuple.Item2));
            }

            return executiveService.Object;
        }

        #endregion
    }
}