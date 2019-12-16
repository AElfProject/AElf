using AElf.Kernel.SmartContract.Application;

namespace AElf.Kernel.Consensus.DPoS.Tests.Application
{
    public class NotAllowEnterTxHubValidationProviderTests : AEDPoSTestBase
    {
        private readonly ISmartContractAddressService _smartContractAddressService;

        public NotAllowEnterTxHubValidationProviderTests()
        {
            _smartContractAddressService = GetRequiredService<ISmartContractAddressService>();
        }
    }
}