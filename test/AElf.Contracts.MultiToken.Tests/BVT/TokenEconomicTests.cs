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
                var result =(await TokenContractStub.Create.SendAsync(new CreateInput
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
                var connectorManagerAddress = await TokenConverterContractStub.GetManagerAddress.CallAsync(new Empty());
                var proposal = new CreateProposalInput
                {
                    OrganizationAddress = connectorManagerAddress,
                    ContractMethodName = nameof(TokenConverterContractStub.SetConnector),
                    ExpiredTime = TimestampHelper.GetUtcNow().AddDays(1),
                    Params = connector.ToByteString(),
                    ToAddress = TokenConverterContractAddress
                };

                var parliamentAuthContractAddress = AsyncHelper.RunSync(() =>
                    DeploySystemSmartContract(30, Codes.Single(kv => kv.Key.Contains("ParliamentAuth")).Value, Hash.FromString("AElf.ContractsName.Parliament"),
                        DefaultKeyPair));
                var ParliamentAuthContractStub =
                    GetTester<ParliamentAuthContractContainer.ParliamentAuthContractStub>(parliamentAuthContractAddress,
                        DefaultKeyPair);
                var initializeResult = await ParliamentAuthContractStub.Initialize.SendAsync(new Empty());
                CheckResult(initializeResult.TransactionResult);
                var createResult = await ParliamentAuthContractStub.CreateProposal.SendAsync(proposal);
                CheckResult(createResult.TransactionResult);

                var proposalHash = Hash.FromMessage(proposal);
                var approveResult = await ParliamentAuthContractStub.Approve.SendAsync(new Acs3.ApproveInput
                {
                    ProposalId = proposalHash,
                });
                CheckResult(approveResult.TransactionResult);
                
                var result = AsyncHelper.RunSync(() =>
                    TokenConverterContractStub.Initialize.SendAsync(new InitializeInput
                    {
                        BaseTokenSymbol = AliceCoinTokenInfo.Symbol,
                        FeeRate = "0.005",
                        ManagerAddress = ManagerAddress,
                        TokenContractAddress = TokenContractAddress,
                        FeeReceiverAddress = ManagerAddress,
                        Connectors = {RamConnector}
                    }));
                CheckResult(result.TransactionResult);
            }
            
        }
        
        

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
                fee.SymbolToAmount.Keys.ShouldNotContain(AliceCoinTokenInfo.Symbol);
            }

            // Set method fee.
            var resultSet = (await feeChargerStub.SetMethodFee.SendAsync(new SetMethodFeeInput
            {
                Method = nameof(TokenContractContainer.TokenContractStub.Transfer),
                SymbolToAmount = {new Dictionary<string, long> {{AliceCoinTokenInfo.Symbol, 10L}}}
            })).TransactionResult;
            resultSet.Status.ShouldBe(TransactionResultStatus.Mined);

            // Check fee.
            {
                var fee = await feeChargerStub.GetMethodFee.CallAsync(
                    new MethodName()
                    {
                        Name = nameof(TokenContractContainer.TokenContractStub.Transfer)
                    });
                fee.SymbolToAmount[AliceCoinTokenInfo.Symbol].ShouldBe(10L);
            }
        }
    }
}