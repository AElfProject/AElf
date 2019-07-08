using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Acs1;
using Acs3;
using AElf.Contracts.MultiToken.Messages;
using AElf.Contracts.ParliamentAuth;
using AElf.Contracts.Profit;
using AElf.Contracts.TokenConverter;
using AElf.Contracts.Treasury;
using AElf.Kernel;
using AElf.Sdk.CSharp;
using AElf.Types;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Shouldly;
using Volo.Abp.Threading;
using Xunit;

namespace AElf.Contracts.MultiToken
{
    public partial class MultiTokenContractTests
    {
        private async Task InitialEconomic()
        {
            {
                var result =
                    await TreasuryContractStub.InitialTreasuryContract.SendAsync(new Empty());
                CheckResult(result.TransactionResult);
            }
            {
                var result =
                    await TreasuryContractStub.InitialMiningRewardProfitItem.SendAsync(
                        new Empty());
                CheckResult(result.TransactionResult);
            }

            {
                var result = (await TokenContractStub.Create.SendAsync(new CreateInput
                {
                    Symbol = AliceCoinTokenInfo.Symbol,
                    TokenName = "Native Token",
                    TotalSupply = AliceCoinTotalAmount,
                    Decimals = 2,
                    IsBurnable = true,
                    Issuer = DefaultAddress,
                    LockWhiteList =
                    {
                        ProfitContractAddress,
                        TreasuryContractAddress
                    }
                })).TransactionResult;
                CheckResult(result);
                await TokenContractStub.Create.SendAsync(new CreateInput
                {
                    Symbol = "AETC",
                    TokenName = "AElf Token Converter Token",
                    TotalSupply = 500_000L,
                    Decimals = 2,
                    Issuer = DefaultAddress,
                    IsBurnable = true,
                    LockWhiteList =
                    {
                        ProfitContractAddress,
                        TreasuryContractAddress
                    }
                });
            }

            {
                var result = AsyncHelper.RunSync(() => TokenContractStub.Issue.SendAsync(new IssueInput()
                {
                    Symbol = AliceCoinTokenInfo.Symbol,
                    Amount = 100_000_000L,
                    To = DefaultAddress,
                    Memo = "Set for token converter."
                }));
                CheckResult(result.TransactionResult);
            }

            {
                var result = AsyncHelper.RunSync(() => TokenContractStub.Issue.SendAsync(new IssueInput()
                {
                    Symbol = AliceCoinTokenInfo.Symbol,
                    Amount = AliceCoinTotalAmount - 100_000_000L,
                    To = TokenContractAddress,
                    Memo = "Set for token converter."
                }));
                CheckResult(result.TransactionResult);
            }

            {
                var result = AsyncHelper.RunSync(() => ProfitContractStub.CreateProfitItem.SendAsync(
                    new CreateProfitItemInput
                    {
                        ProfitReceivingDuePeriodCount = 10
                    }));
                CheckResult(result.TransactionResult);
            }

            {
                var connector = new Connector
                {
                    Symbol = AliceCoinTokenInfo.Symbol,
                    IsPurchaseEnabled = true,
                    Weight = "0.2",
                    IsVirtualBalanceEnabled = true
                };

                var result = AsyncHelper.RunSync(() =>
                    TokenConverterContractStub.Initialize.SendAsync(new TokenConverter.InitializeInput
                    {
                        BaseTokenSymbol = "ELF",
                        FeeRate = "0.005",
                        ManagerAddress = ManagerAddress,
                        TokenContractAddress = TokenContractAddress,
                        FeeReceiverAddress = ManagerAddress,
                        Connectors = {RamConnector}
                    }));
                CheckResult(result.TransactionResult);

                var parliamentAuthContractAddress = AsyncHelper.RunSync(() =>
                    DeploySystemSmartContract(30, Codes.Single(kv => kv.Key.Contains("ParliamentAuth")).Value,
                        Hash.FromString("AElf.ContractsName.Parliament"),
                        DefaultKeyPair));
                var ParliamentAuthContractStub =
                    GetTester<ParliamentAuthContractContainer.ParliamentAuthContractStub>(parliamentAuthContractAddress,
                        DefaultKeyPair);
                var initializeResult = await ParliamentAuthContractStub.Initialize.SendAsync(new ParliamentAuth.InitializeInput());
                CheckResult(initializeResult.TransactionResult);
                var connectorManagerAddress = await TokenConverterContractStub.GetManagerAddress.CallAsync(new Empty());
                var proposal = new CreateProposalInput
                {
                    OrganizationAddress = connectorManagerAddress,
                    ContractMethodName = nameof(TokenConverterContractStub.SetConnector),
                    ExpiredTime = TimestampHelper.GetUtcNow().AddDays(1),
                    Params = connector.ToByteString(),
                    ToAddress = TokenConverterContractAddress
                };
                var createResult = await ParliamentAuthContractStub.CreateProposal.SendAsync(proposal);
                CheckResult(createResult.TransactionResult);

                var proposalHash = Hash.FromMessage(proposal);
                var approveResult = await ParliamentAuthContractStub.Approve.SendAsync(new Acs3.ApproveInput
                {
                    ProposalId = proposalHash,
                });
                CheckResult(approveResult.TransactionResult);


            }

        }

        [Fact(DisplayName = "[MultiToken] MultiToken_ChargeTransactionFees_Test")]
        public async Task MultiTokenContract_ChargeTransactionFees()
        {
            InitialEconomic();
            var result = (await TokenContractStub.ChargeTransactionFees.SendAsync(new ChargeTransactionFeesInput
            {
                SymbolToAmount = {new Dictionary<string, long> {{AliceCoinTokenInfo.Symbol, 10L}}}
            })).TransactionResult;
            result.Status.ShouldBe(TransactionResultStatus.Mined);
            await TokenContractStub.Transfer.SendAsync(new TransferInput
            {
                Symbol = AliceCoinTokenInfo.Symbol,
                Amount = 1000L,
                Memo = "transfer test",
                To = TreasuryContractAddress
            });
            var balanceOutput = await TokenContractStub.GetBalance.CallAsync(new GetBalanceInput
            {
                Owner = DefaultAddress,
                Symbol = AliceCoinTokenInfo.Symbol
            });
            balanceOutput.Balance.ShouldBe(100_000_000L - 1000L - 10L);
        }

//        [Fact]
//        public async Task Claim_Transaction_Fees()
//        {
//            await MultiTokenContract_ChargeTransactionFees();
//
//            var originBalanceOutput = await TokenContractStub.GetBalance.CallAsync(new GetBalanceInput
//            {
//                Owner = TreasuryContractAddress,
//                Symbol = AliceCoinTokenInfo.Symbol
//            });
//            originBalanceOutput.Balance.ShouldBe(10L);
//
//            {
//                var result = (await TokenContractStub.ClaimTransactionFees.SendAsync(new Empty()
//                )).TransactionResult;
//                result.Status.ShouldBe(TransactionResultStatus.Mined);
//            }
//
//            var balanceOutput = await TokenContractStub.GetBalance.CallAsync(new GetBalanceInput
//            {
//                Owner = TreasuryContractAddress,
//                Symbol = AliceCoinTokenInfo.Symbol
//            });
//            balanceOutput.Balance.ShouldBe(10L);
//
//        }

        [Fact]
        public async Task Set_And_Get_Method_Fee()
        {
            await MultiTokenContract_Create();
            var feeChargerStub = GetTester<FeeChargedContractContainer.FeeChargedContractStub>(TokenContractAddress,
                DefaultKeyPair);

            // Fee not set yet.
            {
                var fee = await feeChargerStub.GetMethodFee.CallAsync(
                    new MethodName
                    {
                        Name = nameof(TokenContractContainer.TokenContractStub.Transfer)
                    });
                fee.Amounts.Select(a => a.Symbol).ShouldNotContain(AliceCoinTokenInfo.Symbol);
            }

            // Set method fee.
            var resultSet = (await feeChargerStub.SetMethodFee.SendAsync(new TokenAmounts
            {
                Method = nameof(TokenContractContainer.TokenContractStub.Transfer),
                Amounts = {new TokenAmount {Symbol = AliceCoinTokenInfo.Symbol, Amount = 10L}}
            })).TransactionResult;
            resultSet.Status.ShouldBe(TransactionResultStatus.Mined);

            // Check fee.
            {
                var fee = await feeChargerStub.GetMethodFee.CallAsync(
                    new MethodName
                    {
                        Name = nameof(TokenContractContainer.TokenContractStub.Transfer)
                    });
                fee.Amounts.First(a => a.Symbol == AliceCoinTokenInfo.Symbol).Amount.ShouldBe(10L);
            }
        }
    }
}