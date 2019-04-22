using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Kernel.SmartContractExecution.Scheduling;
using AElf.Modularity;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Volo.Abp.Modularity;

namespace AElf.Kernel.SmartContractExecution
{
    [DependsOn(
        typeof(SmartContractExecutionTestAElfModule)
    )]
    public class SmartContractExecutionGrouperTestAElfModule : AElfModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            var services = context.Services;
            // TODO: Should remove it. Don't mock it.
            services.AddTransient<IResourceUsageDetectionService>(p =>
            {
                var mockService = new Mock<IResourceUsageDetectionService>();
                mockService.Setup(m => m.GetResources(It.IsAny<Transaction>()))
                    .Returns<Transaction>((transaction) =>
                    {
                        var list = new List<string>()
                        {
                            transaction.From.GetFormatted(),
                            transaction.To.GetFormatted()
                        };
                        return Task.FromResult(list.Select(a => a));
                    });
                return mockService.Object;
            });

            services.AddTransient<Grouper>();
        }
    }
}