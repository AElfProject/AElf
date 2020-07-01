using System.Collections.Generic;
using System.Threading.Tasks;
using Acs1;
using Acs3;
using AElf.Contracts.MultiToken;
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
            // method name should not be null
            {
                var ret =
                    await TokenContractStub.ChargeTransactionFees.SendWithExceptionAsync(new ChargeTransactionFeesInput
                    {
                        MethodName = "asd"
                    });
                ret.TransactionResult.Error.ShouldContain("Invalid charge transaction fees input");
            }

            // contract address should not be null
            {
                var ret =
                    await TokenContractStub.ChargeTransactionFees.SendWithExceptionAsync(new ChargeTransactionFeesInput
                    {
                        ContractAddress = TokenContractAddress
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
            {
                var beforeChargeBalance = await GetBalanceAsync(address, nativeTokenSymbol);
                var chargeFeeRet = await TokenContractStub.ChargeTransactionFees.SendAsync(new ChargeTransactionFeesInput
                {
                    ContractAddress = TokenContractAddress,
                    MethodName = methodName,
                    PrimaryTokenSymbol = nativeTokenSymbol
                });
                chargeFeeRet.Output.Value.ShouldBe(true);
                var afterChargeBalance = await GetBalanceAsync(address, nativeTokenSymbol);
                afterChargeBalance.ShouldBeLessThan(beforeChargeBalance);
            }
            
            // input WithOut Primary Token
            {
                var beforeChargeBalance = await GetBalanceAsync(address, nativeTokenSymbol);
                var chargeFeeRet = await TokenContractStub.ChargeTransactionFees.SendAsync(new ChargeTransactionFeesInput
                {
                    ContractAddress = TokenContractAddress,
                    MethodName = methodName
                });
                chargeFeeRet.Output.Value.ShouldBe(true);
                var afterChargeBalance = await GetBalanceAsync(address, nativeTokenSymbol);
                afterChargeBalance.ShouldBe(beforeChargeBalance);
            }
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
            await SendTransactionPassedByDefaultParliamentAsync(TokenContractAddress,
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

            await SendTransactionPassedByDefaultParliamentAsync(TokenContractAddress,
                nameof(TokenContractImplContainer.TokenContractImplStub.SetSymbolsToPayTxSizeFee), sizeFeeSymbolList);

            var beforeBalanceList = await GetDefaultBalancesAsync(orderedSymbolList);
            var chargeTransactionFeesInput = new ChargeTransactionFeesInput
            {
                MethodName = methodName,
                ContractAddress = TokenContractAddress,
                PrimaryTokenSymbol = NativeTokenSymbol,
                TransactionSizeFee = sizeFee,
            };
            chargeTransactionFeesInput.SymbolsToPayTxSizeFee.AddRange(sizeFeeSymbolList.SymbolsToPayTxSizeFee);

            var chargeFeeRet = await TokenContractStub.ChargeTransactionFees.SendAsync(chargeTransactionFeesInput);
            chargeFeeRet.Output.Value.ShouldBe(isSuccess);
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

        private async Task<List<long>> GetDefaultBalancesAsync(string[] tokenSymbolList)
        {
            var balances = new List<long>();
            foreach (var symbol in tokenSymbolList)
                balances.Add(await GetBalanceAsync(DefaultSender,symbol));
            return balances;
        }
        
        private async Task CreateTokenAsync(Address creator, string tokenSymbol)
        {
            await TokenContractStub.Create.SendAsync(new CreateInput
            {
                Symbol = tokenSymbol,
                TokenName = tokenSymbol + " name",
                TotalSupply = 1000_00000000,
                IsBurnable = true,
                Issuer = creator,
                IsProfitable = true
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
        private async Task SetSizeFeeSymbolListAsync(SymbolListToPayTxSizeFee newSizeFeeToken)
        {
            await SendTransactionPassedByDefaultParliamentAsync(TokenContractAddress,
                nameof(TokenContractImplContainer.TokenContractImplStub.SetSymbolsToPayTxSizeFee), newSizeFeeToken);
        }

        // single node
        private async Task SendTransactionPassedByDefaultParliamentAsync(Address contractAddress, string methodName, IMessage input)
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