using System.Linq;
using System.Threading.Tasks;
using Acs1;
using AElf.Types;
using Google.Protobuf.WellKnownTypes;
using Shouldly;
using Xunit;

namespace AElf.Contracts.EconomicSystem.Tests.BVT
{
    public partial class EconomicSystemTest
    {
        private Address Tester => Address.FromPublicKey(InitialCoreDataCenterKeyPairs.First().PublicKey);
        private const string MethodName = "SetMethodFee";
        private MethodFee TokenAmount = new MethodFee
        {
            BasicFee = 5000_0000,
            Symbol = "ELF"
        };

        [Fact]
        public async Task Economic_FeeProvider_Test()
        {
            await ExecuteProposalTransaction(Tester, EconomicContractAddress, MethodName, new MethodFees
            {
                MethodName = nameof(EconomicContractStub.IssueNativeToken),
                Fees = {TokenAmount}
            });
            var result = await EconomicContractStub.GetMethodFee.CallAsync(new StringValue
            {
                Value = nameof(EconomicContractStub.IssueNativeToken)
            });
            result.Fees.First().ShouldBe(TokenAmount);
        }

        [Fact]
        public async Task Genesis_FeeProvider_Test()
        {
            await ExecuteProposalTransaction(Tester, ContractZeroAddress, MethodName, new MethodFees
            {
                MethodName = nameof(BasicContractZeroStub.DeploySmartContract),
                Fees = {TokenAmount}
            });
            var result = await BasicContractZeroStub.GetMethodFee.CallAsync(new StringValue
            {
                Value = nameof(BasicContractZeroStub.DeploySmartContract)
            });
            result.Fees.First().ShouldBe(TokenAmount);
        }

        [Fact]
        public async Task Token_FeeProvider_Test()
        {
            await ExecuteProposalTransaction(Tester, TokenContractAddress, MethodName, new MethodFees
            {
                MethodName = nameof(TokenContractImplStub.Transfer),
                Fees = { TokenAmount}
            });
            var result = await TokenContractImplStub.GetMethodFee.CallAsync(new StringValue
            {
                Value = nameof(TokenContractImplStub.Transfer)
            });
            result.Fees.First().ShouldBe(TokenAmount);
        }
    }
}