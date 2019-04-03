using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Kernel.SmartContract;
using AElf.Modularity;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Volo.Abp.Modularity;

namespace AElf.Kernel.SmartContractExecution
{
    [DependsOn(
        typeof(SmartContractExecutionAElfModule),
        typeof(KernelCoreTestAElfModule)
    )]
    public class SmartContractExecutionTestAElfModule : AElfModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
        }
    }
    
    [DependsOn(
        typeof(SmartContractExecutionAElfModule),
        typeof(KernelCoreTestAElfModule)
    )]
    public class FunctionMetadataTestAElfModule : AElfModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            var services = context.Services;
            
            var functionMetadata = new FunctionMetadata(new HashSet<string>(){"test1", "test2"}, new HashSet<Resource>()
            {
                new Resource {DataAccessMode = DataAccessMode.AccountSpecific, Name = "test1" },
                new Resource {DataAccessMode = DataAccessMode.ReadWriteAccountSharing, Name = "test2" }
            });
            services.AddTransient(o=>
            {
                var mockService = new Mock<IFunctionMetadataService>();
                mockService.Setup(m=>m.GetFunctionMetadata(It.IsAny<string>()))
                    .Returns(Task.FromResult(functionMetadata));
                
                return mockService.Object;
            });
        }
    }
}