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
        public async Task Vote_FeeProvider_Test()
        {
            var registerResult = await VoteContractStub.GetMethodFee.CallAsync(new StringValue
            {
                Value = nameof(VoteContractStub.Register)
            });
            registerResult.Fees.First().ShouldBe(new MethodFee
            {
                BasicFee = 10_00000000,
                Symbol = "ELF"
            });
            
            await ExecuteProposalTransaction(Tester, VoteContractAddress, MethodName, new MethodFees
            {
                MethodName = nameof(VoteContractStub.Register),
                Fees = {TokenAmount}
            });
            var result = await VoteContractStub.GetMethodFee.CallAsync(new StringValue
            {
                Value = nameof(VoteContractStub.Register)
            });
            result.Fees.First().ShouldBe(TokenAmount);
        }
        
        [Fact]
        public async Task Treasury_FeeProvider_Test()
        {
            await ExecuteProposalTransaction(Tester, TreasuryContractAddress, MethodName, new MethodFees
            {
                MethodName = nameof(TreasuryContractStub.Donate),
                Fees = {TokenAmount}
            });
            var result = await TreasuryContractStub.GetMethodFee.CallAsync(new StringValue
            {
                Value = nameof(TreasuryContractStub.Donate)
            });
            result.Fees.First().ShouldBe(TokenAmount);
        }

        [Fact]
        public async Task Election_FeeProvider_Test()
        {
            await ExecuteProposalTransaction(Tester, ElectionContractAddress, MethodName, new MethodFees
            {
                MethodName = nameof(ElectionContractStub.Vote),
                Fees = {TokenAmount}
            });
            var result = await ElectionContractStub.GetMethodFee.CallAsync(new StringValue
            {
                Value = nameof(ElectionContractStub.Vote)
            });
            result.Fees.First().ShouldBe(TokenAmount);
        }

        [Fact]
        public async Task Parliament_FeeProvider_Test()
        {
            await ExecuteProposalTransaction(Tester, ParliamentContractAddress, MethodName, new MethodFees
            {
                MethodName = nameof(ParliamentContractStub.Approve),
                Fees = {TokenAmount}
            });
            var result = await ParliamentContractStub.GetMethodFee.CallAsync(new StringValue
            {
                Value = nameof(ParliamentContractStub.Approve)
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
        public async Task TokenConverter_FeeProvider_Test()
        {
            await ExecuteProposalTransaction(Tester, TokenConverterContractAddress, MethodName, new MethodFees
            {
                MethodName = nameof(TokenConverterContractStub.Buy),
                Fees = {TokenAmount}
            });
            var result = await TokenConverterContractStub.GetMethodFee.CallAsync(new StringValue
            {
                Value = nameof(TokenConverterContractStub.Buy)
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