using System.Threading.Tasks;
using AElf.CSharp.CodeOps;
using AElf.Kernel.CodeCheck.Infrastructure;
using AElf.Kernel.Configuration;
using AElf.Modularity;
using Google.Protobuf;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Moq;
using Volo.Abp.Modularity;

namespace AElf.Kernel.CodeCheck.Tests;

[DependsOn(
    typeof(CSharpCodeOpsAElfModule),
    typeof(CodeCheckTestAElfModule))]
public class CodeCheckParallelTestAElfModule : AElfModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        context.Services.RemoveAll<IContractAuditor>();
        context.Services.AddTransient<IContractAuditor, CSharpContractAuditor>();
        
        context.Services.AddTransient(provider =>
        {
            var mockService = new Mock<IConfigurationService>();
            mockService.Setup(m =>
                    m.GetConfigurationDataAsync(It.IsAny<string>(),
                        It.IsAny<ChainContext>()))
                .Returns<string, ChainContext>((configurationName, chainContext) =>
                {
                    if (configurationName == CodeCheckConstant.RequiredAcsInContractsConfigurationName)
                        return Task.FromResult(new RequiredAcsInContracts
                        {
                            AcsList = { CodeCheckConstant.Acs1},
                            RequireAll = CodeCheckConstant.IsRequireAllAcs
                        }.ToByteString());
                    return null;
                });
            return mockService.Object;
        });
    }
}