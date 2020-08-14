using System.Threading.Tasks;
using AElf.Kernel.CodeCheck.Infrastructure;
using AElf.Kernel.Configuration;
using AElf.Modularity;
using Google.Protobuf;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Volo.Abp.Modularity;

namespace AElf.Kernel.CodeCheck.Tests
{
    [DependsOn(
        typeof(KernelTestAElfModule),
        typeof(CodeCheckAElfModule))]
    public class CodeCheckTestAElfModule : AElfModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            context.Services.AddSingleton<IContractAuditor, CustomizeAlwaysSuccessContractAuditor>();
            Configure<CodeCheckOptions>(options => { options.CodeCheckEnabled = true; });
            context.Services.AddTransient(provider =>
            {
                var mockService = new Mock<IConfigurationService>();
                mockService.Setup(m =>
                        m.GetConfigurationDataAsync(It.IsAny<string>(),
                            It.IsAny<ChainContext>()))
                    .Returns<string, ChainContext>((configurationName, chainContext) =>
                    {
                        if (configurationName == CodeCheckConstant.RequiredAcsInContractsConfigurationName)
                        {
                            return Task.FromResult(new RequiredAcsInContracts
                            {
                                AcsList = {CodeCheckConstant.Acs1, CodeCheckConstant.Acs2},
                                RequireAll = CodeCheckConstant.IsRequireAllAcs
                            }.ToByteString());
                        }
                        return null;
                    });
                return mockService.Object;
            });

        }
    }
}