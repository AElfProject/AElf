using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Common;
using AElf.Database;
using AElf.Kernel.Blockchain.Application;
using AElf.Kernel.Infrastructure;
using AElf.Modularity;
using AElf.TestBase;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Volo.Abp;
using Volo.Abp.EventBus;
using Volo.Abp.Modularity;

namespace AElf.Kernel
{
    [DependsOn(
        typeof(KernelCoreTestAElfModule))]
    public class KernelCoreBlockValidationTestAElfModule : AElfModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            var services = context.Services;
            services.AddTransient<BlockValidationProvider>();

//            services.AddTransient<IBlockValidationService>(p =>
//            {
//                var mockBlockValidationService = new Mock<IBlockValidationService>();
//                mockBlockValidationService
//                    .Setup(m => m.ValidateBlockBeforeExecuteAsync( It.IsAny<Block>()))
//                    .Returns<int, Block>((chainId, block) =>
//                        Task.FromResult(block?.Header != null && block.Body != null));
//                mockBlockValidationService
//                    .Setup(m => m.ValidateBlockAfterExecuteAsync(It.IsAny<Block>()))
//                    .Returns<int, Block>((chainId, block) => Task.FromResult(true));
//                return mockBlockValidationService.Object;
//            });
        }

        public override void OnPreApplicationInitialization(ApplicationInitializationContext context)
        {

        }
    }
}