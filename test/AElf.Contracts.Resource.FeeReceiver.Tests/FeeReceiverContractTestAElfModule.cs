using AElf.Contracts.TestBase;
using AElf.Kernel;
using Volo.Abp.Modularity;

namespace AElf.Contracts.Resource.FeeReceiver
{
    [DependsOn(typeof(ContractTestAElfModule))]
    public class FeeReceiverContractTestAElfModule : ContractTestAElfModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            Configure<ChainOptions>(o => { o.ChainId = ChainHelper.ConvertBase58ToChainId("AELF"); });
        }
    }
}