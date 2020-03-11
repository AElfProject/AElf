using System.Linq;
using System.Threading.Tasks;
using Acs1;
using AElf.Contracts.MultiToken;
using AElf.Contracts.TokenConverter;
using AElf.Contracts.TokenHolder;
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

        [Fact]
        public async Task TokenHolder_FeeProvider_Test()
        {
            await ExecuteProposalTransaction(Tester, TokenHolderContractAddress, MethodName, new MethodFees
            {
                MethodName = nameof(TokenHolderContractContainer.TokenHolderContractStub.Withdraw),
                Fees = { TokenAmount}
            });
            var result = await TokenHolderStub.GetMethodFee.CallAsync(new StringValue
            {
                Value = nameof(TokenHolderContractContainer.TokenHolderContractStub.Withdraw)
            });
            result.Fees.First().ShouldBe(TokenAmount);
        }

        [Fact]
        public async Task Consensus_FeeProvider_Test()
        {
            await ExecuteProposalTransaction(Tester, ConsensusContractAddress, MethodName, new MethodFees
            {
                MethodName = nameof(AEDPoSContractStub.UpdateValue),
                Fees = { TokenAmount}
            });
            var result = await AedPoSContractImplStub.GetMethodFee.CallAsync(new StringValue
            {
                Value = nameof(AEDPoSContractStub.UpdateValue)
            });
            result.Fees.First().ShouldBe(TokenAmount);
        }
        
        [Fact]
        public async Task ChargeTransactionFees_Test()
        {
            await Token_FeeProvider_Test();
            var beforeBalance = 5000_00000000L;
            await TokenConverterContractStub.Buy.SendAsync(new BuyInput
            {
                Symbol = "CPU",
                Amount = beforeBalance
            });
            const long txFee = 4000_0000L;
            var transactionFeeInput = new ChargeTransactionFeesInput
            {
                ContractAddress = TokenContractAddress,
                MethodName = nameof(TokenContractStub.Transfer),
                PrimaryTokenSymbol = "ELF",
                TransactionSizeFee = txFee,
                SymbolsToPayTxSizeFee =
                {
                    new SymbolToPayTxSizeFee
                    {
                        TokenSymbol = "CPU",
                        BaseTokenWeight = 1,
                        AddedTokenWeight = 50
                    },
                    new SymbolToPayTxSizeFee
                    {
                        TokenSymbol = "ELF",
                        BaseTokenWeight = 1,
                        AddedTokenWeight = 1
                    }
                }
            };
            var transactionResult = await TokenContractStub.ChargeTransactionFees.SendAsync(transactionFeeInput);
            transactionResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);

            var balance = await TokenContractStub.GetBalance.CallAsync(new GetBalanceInput
            {
                Owner = BootMinerAddress,
                Symbol = "CPU"
            });
            balance.Balance.ShouldBe(beforeBalance - txFee * 50);
        }
    }
}