using AElf.Kernel.Blockchain.Application;
using AElf.Kernel.CodeCheck.Application;
using AElf.Kernel.CodeCheck.Infrastructure;
using AElf.Kernel.SmartContract.Application;
using AElf.Modularity;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.Modularity;

namespace AElf.Kernel.CodeCheck
{
    public class CodeCheckAElfModule : AElfModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            context.Services
                .AddSingleton<IBlocksExecutionSucceededLogEventProcessor, CodeCheckRequiredLogEventProcessor>();
            context.Services.AddSingleton<IContractAuditorContainer, ContractAuditorContainer>();
            context.Services.AddSingleton<IBlockValidationProvider, CodeCheckValidationProvider>();
        }
    }
}