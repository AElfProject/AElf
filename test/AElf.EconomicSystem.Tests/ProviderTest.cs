using System.Collections.Generic;
using System.Linq;
using AElf.Kernel.SmartContract.Application;
using Shouldly;
using Xunit;

namespace AElf.EconomicSystem.Tests
{
    public class ProviderTest : EconomicSystemTestBase
    {
        #region EconomicContractInitializationProvider Test

        [Fact]
        public void EconomicContractInitializationProvider_Test()
        {
            var contractInitializationProvider =
                GetContractInitializationProvider<EconomicContractInitializationProvider>();
            var methodList = contractInitializationProvider.GetInitializeMethodList(null);
            methodList.Count.ShouldBe(2);
        }

        #endregion

        #region EconomicSmartContractAddressNameProvider Test

        [Fact]
        public void EconomicSmartContractAddressNameProvider_Test()
        {
            var economicSmartContractAddressNameProvider = new EconomicSmartContractAddressNameProvider();
            var targetName = HashHelper.ComputeFrom("AElf.ContractNames.Economic");
            economicSmartContractAddressNameProvider.ContractName.ShouldBe(targetName);
        }

        #endregion

        #region TokenHolderContractInitializationProvider Test

        [Fact]
        public void TokenHolderContractInitializationProvider_Test()
        {
            var contractInitializationProvider =
                GetContractInitializationProvider<TokenHolderContractInitializationProvider>();
            contractInitializationProvider.GetInitializeMethodList(null).Count.ShouldBe(0);
        }

        #endregion

        #region  TokenHolderSmartContractAddressNameProvider Test

        [Fact]
        public void TokenHolderSmartContractAddressNameProvider_Test()
        {
            var tokenHolderSmartContractAddressNameProvider = new TokenHolderSmartContractAddressNameProvider();
            var targetName = HashHelper.ComputeFrom("AElf.ContractNames.TokenHolder");
            tokenHolderSmartContractAddressNameProvider.ContractName.ShouldBe(targetName);
        }

        #endregion

        private IContractInitializationProvider GetContractInitializationProvider<T>()
            where T : IContractInitializationProvider
        {
            var contractInitializationProviders = GetRequiredService<IEnumerable<IContractInitializationProvider>>();
            var contractInitializationProvider =
                contractInitializationProviders.SingleOrDefault(x =>
                    x.GetType() == typeof(T));
            contractInitializationProvider.ShouldNotBeNull();
            return contractInitializationProvider;
        }
    }
}