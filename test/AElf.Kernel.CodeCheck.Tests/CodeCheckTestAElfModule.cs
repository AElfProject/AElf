using System.Threading.Tasks;
using AElf.Kernel.Configuration;
using AElf.Kernel.Proposal.Infrastructure;
using AElf.Kernel.SmartContract.Application;
using AElf.Modularity;
using Google.Protobuf;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
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
            //IProposalProvider is used to check code check ProcessAsync
            context.Services.RemoveAll<IProposalProvider>();
            context.Services.AddSingleton<IProposalProvider, ProposalProvider>();
            context.Services.AddSingleton<ILogEventProcessor, CodeCheckRequiredLogEventProcessor>();
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