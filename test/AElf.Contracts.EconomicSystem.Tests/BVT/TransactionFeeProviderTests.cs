using System.Linq;
using System.Threading.Tasks;
using Acs1;
using AElf.Types;
using Shouldly;
using Xunit;

namespace AElf.Contracts.EconomicSystem.Tests.BVT
{
    public partial class EconomicSystemTest
    {
        private Address Tester => Address.FromPublicKey(InitialCoreDataCenterKeyPairs.First().PublicKey);
        private const string MethodName = "SetMethodFee";
        private TokenAmount TokenAmount = new TokenAmount
        {
            Amount = 5000_0000,
            Symbol = "ELF"
        };

        [Fact]
        public async Task Economic_FeeProvider_Test()
        {
            await ExecuteProposalTransaction(Tester, EconomicContractAddress, MethodName, new TokenAmounts
            {
                Method = nameof(EconomicContractStub.IssueNativeToken),
                Amounts = {TokenAmount}
            });
            var result = await EconomicContractStub.GetMethodFee.CallAsync(new MethodName
            {
                Name = nameof(EconomicContractStub.IssueNativeToken)
            });
            result.Amounts.First().ShouldBe(TokenAmount);
        }

        [Fact]
        public async Task Genesis_FeeProvider_Test()
        {
            await ExecuteProposalTransaction(Tester, ContractZeroAddress, MethodName, new TokenAmounts
            {
                Method = nameof(BasicContractZeroStub.DeploySmartContract),
                Amounts = {TokenAmount}
            });
            var result = await BasicContractZeroStub.GetMethodFee.CallAsync(new MethodName
            {
                Name = nameof(BasicContractZeroStub.DeploySmartContract)
            });
            result.Amounts.First().ShouldBe(TokenAmount);
        }

        [Fact]
        public async Task Configuration_FeeProvider_Test()
        {
            
        }
    }
}