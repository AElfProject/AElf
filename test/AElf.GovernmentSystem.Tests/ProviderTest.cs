using System.Collections.Generic;
using System.Linq;
using AElf.Kernel.SmartContract.Application;
using Shouldly;
using Xunit;

namespace AElf.GovernmentSystem.Tests
{
    public class ProviderTest : GovernmentSystemTestBase
    {
        #region ElectionContractInitializationProvider Test

        [Fact]
        public void ElectionContractInitializationProvider_Test()
        {
            var contractInitializationProvider =
                GetContractInitializationProvider<ElectionContractInitializationProvider>();
            var initializeMethodList = contractInitializationProvider.GetInitializeMethodList(null);
            initializeMethodList.Count.ShouldBe(1);
        }

        #endregion

        #region ElectionSmartContractAddressNameProvider Test

        [Fact]
        public void ElectionSmartContractAddressNameProvider_Test()
        {
            var targetName = HashHelper.ComputeFrom("AElf.ContractNames.Election");
            var electionSmartContractAddressNameProvider = new ElectionSmartContractAddressNameProvider();
            electionSmartContractAddressNameProvider.ContractName.ShouldBe(targetName);
        }

        #endregion

        #region VoteContractInitializationProvider Test

        [Fact]
        public void VoteContractInitializationProvider_Test()
        {
            var contractInitializationProvider =
                GetContractInitializationProvider<VoteContractInitializationProvider>();
            contractInitializationProvider.GetInitializeMethodList(null).Count.ShouldBe(0);
        }

        #endregion

        #region VoteSmartContractAddressNameProvider Test

        [Fact]
        public void VoteSmartContractAddressNameProvider_Test()
        {
            var targetName = HashHelper.ComputeFrom("AElf.ContractNames.Vote");
            var voteSmartContractAddressNameProvider = new VoteSmartContractAddressNameProvider();
            voteSmartContractAddressNameProvider.ContractName.ShouldBe(targetName);
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