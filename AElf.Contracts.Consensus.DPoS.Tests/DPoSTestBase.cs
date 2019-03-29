using AElf.Common;
using AElf.Contracts.TestKit;
using AElf.Kernel.SmartContract.Application;
using Microsoft.Extensions.DependencyInjection;

namespace AElf.Contracts.Consensus.DPoS
{
    public class DPoSTestBase : ContractTestBase<DPoSTestAElfModule>
    {
        protected ISmartContractAddressService ContractAddressService =>
            Application.ServiceProvider.GetRequiredService<ISmartContractAddressService>();
        private Address ContractZeroAddress => ContractAddressService.GetZeroSmartContractAddress();
    }
}