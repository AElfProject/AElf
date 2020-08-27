using System.Threading.Tasks;
using AElf.Kernel.SmartContract.Infrastructure;
using AElf.Types;
using Shouldly;
using Xunit;

namespace AElf.Kernel.SmartContractExecution.Application
{
    public sealed class SmartContractRegistrationInStateProviderTests : SmartContractExecutionTestBase
    {
        private readonly ISmartContractRegistrationInStateProvider _smartContractRegistrationInStateProvider;
        private readonly IDefaultContractZeroCodeProvider _defaultContractZeroCodeProvider;
        private readonly SmartContractExecutionHelper _smartContractExecutionHelper;

        public SmartContractRegistrationInStateProviderTests()
        {
            _smartContractRegistrationInStateProvider = GetRequiredService<ISmartContractRegistrationInStateProvider>();
            _defaultContractZeroCodeProvider = GetRequiredService<IDefaultContractZeroCodeProvider>();
            _smartContractExecutionHelper = GetRequiredService<SmartContractExecutionHelper>();
        }

        [Fact]
        public async Task GetSmartContractRegistrationAsync_Test()
        {
            var chain = await _smartContractExecutionHelper.CreateChainAsync();
            var chainContext = new ChainContext
            {
                BlockHash = chain.BestChainHash,
                BlockHeight = chain.BestChainHeight
            };
            var smartContractRegistration = await _smartContractRegistrationInStateProvider.GetSmartContractRegistrationAsync(chainContext,
                SampleAddress.AddressList[0]);
            smartContractRegistration.ShouldBe(new SmartContractRegistration());
            
            smartContractRegistration = await _smartContractRegistrationInStateProvider.GetSmartContractRegistrationAsync(chainContext,
                _defaultContractZeroCodeProvider.ContractZeroAddress);
            smartContractRegistration.Category.ShouldBe(_defaultContractZeroCodeProvider.DefaultContractZeroRegistration.Category);
            smartContractRegistration.Code.ShouldBe(_defaultContractZeroCodeProvider.DefaultContractZeroRegistration.Code);
            smartContractRegistration.CodeHash.ShouldBe(_defaultContractZeroCodeProvider.DefaultContractZeroRegistration.CodeHash);
        }
    }
}