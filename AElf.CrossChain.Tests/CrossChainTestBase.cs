using AElf.Kernel.Account.Application;
using AElf.Kernel.SmartContract.Application;
using AElf.TestBase;
using Volo.Abp;
using Volo.Abp.EventBus.Local;

namespace AElf.CrossChain
{
    public class CrossChainTestBase : AElfIntegratedTest<CrossChainTestModule>
    {
        protected ILocalEventBus LocalEventBus;
        protected ISmartContractExecutiveService SmartContractExecutiveService;
        protected IAccountService AccountService;
        protected CrossChainTestBase()
        {
            LocalEventBus = GetRequiredService<ILocalEventBus>();
            SmartContractExecutiveService = GetRequiredService<ISmartContractExecutiveService>();
            AccountService = GetRequiredService<IAccountService>();
        }

        protected override void SetAbpApplicationCreationOptions(AbpApplicationCreationOptions options)
        {
            options.UseAutofac();
        }
    }
}