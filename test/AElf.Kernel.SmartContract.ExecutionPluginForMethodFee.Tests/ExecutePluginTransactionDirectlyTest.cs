using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Standards.ACS1;
using AElf.Standards.ACS3;
using AElf.Contracts.MultiToken;
using AElf.CSharp.Core;
using AElf.CSharp.Core.Extension;
using AElf.Types;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Shouldly;
using Xunit;

namespace AElf.Kernel.SmartContract.ExecutionPluginForMethodFee.Tests
{
    public class ExecutePluginTransactionDirectlyTest: ExecutePluginTransactionDirectlyForMethodFeeTestBase
    {
        [Fact]
        public async Task ChargeTransactionFees_Invalid_Input_Test()
        {
            // contract address should not be null
            {
                var ret =
                    await TokenContractStub.ChargeTransactionFees.SendWithExceptionAsync(new ChargeTransactionFeesInput
                    {
                        MethodName = "asd"
                    });
                ret.TransactionResult.Error.ShouldContain("Invalid charge transaction fees input");
            }
        }
        
        [Fact]
        public async Task ChargeTransactionFees_Without_Primary_Token_Test()
        {
            await IssueTokenAsync(NativeTokenSymbol, 100000_00000000);
            var address = DefaultSender;
            var nativeTokenSymbol = NativeTokenSymbol;
            var methodName = nameof(TokenContractContainer.TokenContractStub.Create);
            
            // input With Primary Token
            await SetPrimaryTokenSymbolAsync();
            var beforeChargeBalance = await GetBalanceAsync(address, nativeTokenSymbol);
            var chargeFeeRet = await TokenContractStub.ChargeTransactionFees.SendAsync(new ChargeTransactionFeesInput
            {
                ContractAddress = TokenContractAddress,
                MethodName = methodName,
            });
            chargeFeeRet.Output.Success.ShouldBe(true);
            var afterChargeBalance = await GetBalanceAsync(address, nativeTokenSymbol);
            afterChargeBalance.ShouldBeLessThan(beforeChargeBalance);
        }

        [Fact]
        public async Task Set_Repeat_Token_Test()
        {
            await IssueTokenAsync(NativeTokenSymbol, 100000_00000000);
            await SetPrimaryTokenSymbolAsync();
            var address = DefaultSender;
            var methodName = nameof(TokenContractContainer.TokenContractStub.Transfer);
            var basicMethodFee = 1000;
            var methodFee = new MethodFees
            {
                MethodName = methodName,
                Fees =
                {
                    new MethodFee
                    {
                        Symbol = NativeTokenSymbol,
                        BasicFee = basicMethodFee
                    },
                    new MethodFee
                    {
                        Symbol = NativeTokenSymbol,
                        BasicFee = basicMethodFee
                    }
                }
            };
            var sizeFee = 0;
            await SubmitAndPassProposalOfDefaultParliamentAsync(TokenContractAddress,
                nameof(TokenContractImplContainer.TokenContractImplStub.SetMethodFee), methodFee);
            var beforeChargeBalance = await GetBalanceAsync(address, NativeTokenSymbol);
            var chargeTransactionFeesInput = new ChargeTransactionFeesInput
            {
                MethodName = methodName,
                ContractAddress = TokenContractAddress,
                TransactionSizeFee = sizeFee,
            };

            var chargeFeeRet = await TokenContractStub.ChargeTransactionFees.SendAsync(chargeTransactionFeesInput);
            chargeFeeRet.Output.Success.ShouldBeTrue();
            var afterChargeBalance = await GetBalanceAsync(address, NativeTokenSymbol);
            beforeChargeBalance.Sub(afterChargeBalance).ShouldBe(basicMethodFee.Add(basicMethodFee));
        }
        

        // 1 => ELF  2 => CWJ  3 => YPA   method fee : native token: 1000
        [Theory]
        [InlineData(new []{1,2,3}, new []{10000L, 0, 0}, new []{1,1,1}, new []{1,1,1}, 1000, "ELF", 2000, true)]
        [InlineData(new []{2,1,3}, new []{10000L, 10000L, 0}, new []{1,1,1}, new []{1,1,1}, 1000, "CWJ", 1000, true)]
        [InlineData(new []{2,1,3}, new []{10000L, 10000L, 0}, new []{1,1,1}, new []{2,1,1}, 1000, "CWJ", 2000, true)]
        [InlineData(new []{2,1,3}, new []{10000L, 10000L, 0}, new []{4,1,1}, new []{2,1,1}, 1000, "CWJ", 500, true)]
        [InlineData(new []{2,1,3}, new []{100L, 1000L, 0}, new []{1,1,1}, new []{1,1,1}, 1000, "CWJ", 100, false)]
        [InlineData(new []{3,1,2}, new []{10L, 1000L, 100}, new []{1,1,1}, new []{1,1,1}, 1000, "YPA", 10, false)]
        public async Task ChargeTransactionFees_With_Different_Transaction_Size_Fee_Token(int[] order, long[] balance,
            int[] baseWeight, int[] tokenWeight, long sizeFee, string chargeSymbol, long chargeAmount, bool isSuccess)
        {
            await SetPrimaryTokenSymbolAsync();

            var methodName = nameof(TokenContractContainer.TokenContractStub.Transfer);
            var basicMethodFee = 1000;
            var methodFee = new MethodFees
            {
                MethodName = methodName,
                Fees =
                {
                    new MethodFee
                    {
                        Symbol = NativeTokenSymbol,
                        BasicFee = basicMethodFee
                    }
                }
            };
            await SubmitAndPassProposalOfDefaultParliamentAsync(TokenContractAddress,
                nameof(TokenContractImplContainer.TokenContractImplStub.SetMethodFee), methodFee);
            var tokenSymbolList = new [] {NativeTokenSymbol, "CWJ", "YPA"};
            var tokenCount = 3;
            var orderedSymbolList = new string[tokenCount];
            var index = 0;
            foreach (var o in order)
            {
                orderedSymbolList[index++] = tokenSymbolList[o - 1];
            }

            var sizeFeeSymbolList = new SymbolListToPayTxSizeFee();
            for (var i = 0; i < tokenCount; i++)
            {
                var tokenSymbol = orderedSymbolList[i];
                if (tokenSymbol != NativeTokenSymbol)
                    await CreateTokenAsync(DefaultSender, tokenSymbol);
                if(balance[i] > 0)
                    await IssueTokenAsync(tokenSymbol, balance[i]);
                sizeFeeSymbolList.SymbolsToPayTxSizeFee.Add(new SymbolToPayTxSizeFee
                {
                    TokenSymbol = tokenSymbol,
                    AddedTokenWeight = tokenWeight[i],
                    BaseTokenWeight = baseWeight[i]
                });
            }

            await SubmitAndPassProposalOfDefaultParliamentAsync(TokenContractAddress,
                nameof(TokenContractImplContainer.TokenContractImplStub.SetSymbolsToPayTxSizeFee), sizeFeeSymbolList);

            var beforeBalanceList = await GetDefaultBalancesAsync(orderedSymbolList);
            var chargeTransactionFeesInput = new ChargeTransactionFeesInput
            {
                MethodName = methodName,
                ContractAddress = TokenContractAddress,
                TransactionSizeFee = sizeFee,
            };
            chargeTransactionFeesInput.SymbolsToPayTxSizeFee.AddRange(sizeFeeSymbolList.SymbolsToPayTxSizeFee);

            var chargeFeeRet = await TokenContractStub.ChargeTransactionFees.SendAsync(chargeTransactionFeesInput);
            chargeFeeRet.Output.Success.ShouldBe(isSuccess);
            var afterBalanceList = await GetDefaultBalancesAsync(orderedSymbolList);
            for (var i = 0; i < tokenCount; i++)
            {
                var balanceDiff = beforeBalanceList[i] - afterBalanceList[i];
                if(orderedSymbolList[i] == chargeSymbol)
                    balanceDiff.ShouldBe(chargeAmount);
                else
                {
                    if (orderedSymbolList[i] == NativeTokenSymbol)
                        balanceDiff -= basicMethodFee;
                    balanceDiff.ShouldBe(0);
                }
            }
        }
        
        [Theory]
        [InlineData(new[] {100L, 100, 100}, new[] {100L, 100, 100}, new[] {0L, 0, 0}, true)]
        [InlineData(new[] {100L, 100, 100}, new[] {20L, 30, 40}, new[] {80L, 70, 60}, true)]
        [InlineData(new[] {100L, 100, 100}, new[] {120L, 130, 140}, new[] {0L, 0, 0}, true)]
        [InlineData(new[] {100L, 100, 100}, new[] {100L, 100, 100}, new[] {0L, 0, 0}, false)]
        [InlineData(new[] {100L, 100, 100}, new[] {20L, 30, 40}, new[] {80L, 70, 60}, false)]
        [InlineData(new[] {100L, 100, 100}, new[] {120L, 130, 140}, new[] {0L, 0, 0}, false)]
        public async Task DonateResourceToken_Test(long[] initialBalances, long[] tokenFee, long[] lastBalances,
            bool isMainChain)
        {
            var symbolList = new [] {"WEO", "CWJ", "YPA"};
            var feeMap = new TotalResourceTokensMaps();
            for (var i = 0; i < symbolList.Length; i++)
            {
                await CreateTokenAsync(DefaultSender, symbolList[i]);
                await IssueTokenAsync(symbolList[i], initialBalances[i]);
                feeMap.Value.Add(new ContractTotalResourceTokens
                {
                    ContractAddress = DefaultSender,
                    TokensMap = new TotalResourceTokensMap
                    {
                        Value = { {symbolList[i], tokenFee[i]}}
                    }
                });
            }

            if (!isMainChain)
            {
                var defaultParliament = await ParliamentContractStub.GetDefaultOrganizationAddress.CallAsync(new Empty());
                await SubmitAndPassProposalOfDefaultParliamentAsync(TokenContractAddress,
                    nameof(TokenContractContainer.TokenContractStub.InitializeFromParentChain),
                    new InitializeFromParentChainInput
                    {
                        Creator = defaultParliament
                    });
            }
            await TokenContractStub.DonateResourceToken.SendAsync(feeMap);

            for (var i = 0; i < symbolList.Length; i++)
            {
                var balance = await GetBalanceAsync(DefaultSender,symbolList[i]);
                balance.ShouldBe(lastBalances[i]);
                if (!isMainChain)
                {
                    var consensusBalance = await GetBalanceAsync(ConsensusContractAddress, symbolList[i]);
                    consensusBalance.ShouldBe(initialBalances[i] - lastBalances[i]);
                }
            }
        }

        [Fact]
        public async Task ClaimTransactionFee_Balance_WithOut_Receiver_Test()
        {
            var tokenSymbol = "JAN";
            var feeAmount = 10000;
            await CreateTokenAsync(DefaultSender, tokenSymbol);
            var beforeBurned = await GetTokenSupplyAmount(tokenSymbol);
            var claimFeeInput = new TotalTransactionFeesMap
            {
                Value =
                {
                    {tokenSymbol, feeAmount}
                }
            };
            await TokenContractStub.ClaimTransactionFees.SendAsync(claimFeeInput);
            var afterBurned = await GetTokenSupplyAmount(tokenSymbol);
            (beforeBurned - afterBurned).ShouldBe(feeAmount);
        }
        
        [Fact]
        public async Task ClaimTransactionFee_Balance_With_Receiver_Test()
        {
            var tokenSymbol = "JAN";
            var feeAmount = 10000;
            await CreateTokenAsync(DefaultSender, tokenSymbol);
            var receiver = await ParliamentContractStub.GetDefaultOrganizationAddress.CallAsync(new Empty());
            var input = new InitializeFromParentChainInput
            {
                Creator = receiver
            };
            await SubmitAndPassProposalOfDefaultParliamentAsync(TokenContractAddress,
                nameof(TokenContractImplContainer.TokenContractImplStub.InitializeFromParentChain), input);
            await SubmitAndPassProposalOfDefaultParliamentAsync(TokenContractAddress,
                nameof(TokenContractImplContainer.TokenContractImplStub.SetFeeReceiver), receiver);
            var beforeBurned = await GetTokenSupplyAmount(tokenSymbol);
            var beforeBalance = await GetBalanceAsync(receiver, tokenSymbol);
            var claimFeeInput = new TotalTransactionFeesMap
            {
                Value =
                {
                    {tokenSymbol, feeAmount}
                }
            };
            await TokenContractStub.ClaimTransactionFees.SendAsync(claimFeeInput);
            var afterBurned = await GetTokenSupplyAmount(tokenSymbol);
            var afterBalance = await GetBalanceAsync(receiver, tokenSymbol);
            var shouldBurned = feeAmount.Div(10);
            (beforeBurned - afterBurned).ShouldBe(shouldBurned);
            (afterBalance - beforeBalance).ShouldBe(feeAmount - shouldBurned);
        }

        private async Task<long> GetTokenSupplyAmount(string tokenSymbol)
        {
            var tokenInfo = await TokenContractStub.GetTokenInfo.CallAsync(new GetTokenInfoInput
            {
                Symbol = tokenSymbol
            });
            return tokenInfo.Supply;
        }

        private async Task<List<long>> GetDefaultBalancesAsync(string[] tokenSymbolList)
        {
            var balances = new List<long>();
            foreach (var symbol in tokenSymbolList)
                balances.Add(await GetBalanceAsync(DefaultSender,symbol));
            return balances;
        }

        private async Task CreateTokenAsync(Address creator, string tokenSymbol, bool isBurned = true)
        {
            await TokenContractStub.Create.SendAsync(new CreateInput
            {
                Symbol = tokenSymbol,
                TokenName = tokenSymbol + " name",
                TotalSupply = 1000_00000000,
                IsBurnable = isBurned,
                Issuer = creator,
            });
        }

        private async Task IssueTokenAsync(string tokenSymbol, long amount)
        {
            var issueResult = await TokenContractStub.Issue.SendAsync(new IssueInput()
            {
                Symbol = tokenSymbol,
                Amount = amount,
                To = DefaultSender,
            });
            issueResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
        }

        // single node
        private async Task SubmitAndPassProposalOfDefaultParliamentAsync(Address contractAddress, string methodName, IMessage input)
        {
            var defaultParliament = await ParliamentContractStub.GetDefaultOrganizationAddress.CallAsync(new Empty());
            var proposal = new CreateProposalInput
            {
                OrganizationAddress = defaultParliament,
                ToAddress = contractAddress,
                Params = input.ToByteString(),
                ContractMethodName = methodName,
                ExpiredTime = TimestampHelper.GetUtcNow().AddHours(1)
            };
            var createProposalRet = await ParliamentContractStub.CreateProposal.SendAsync(proposal);
            createProposalRet.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
            var proposalId = createProposalRet.Output;
            await ParliamentContractStub.Approve.SendAsync(proposalId);
            var releaseRet = await ParliamentContractStub.Release.SendAsync(proposalId);
            releaseRet.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
        }

        private async Task<long> GetBalanceAsync(Address address, string tokenSymbol)
        {
            var balance = await TokenContractStub.GetBalance.CallAsync(new GetBalanceInput
            {
                Symbol = tokenSymbol,
                Owner = address
            });
            return balance.Balance;
        }
        
    }
}