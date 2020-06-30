using System.Threading.Tasks;
using Acs3;
using AElf.CSharp.Core.Extension;
using AElf.Kernel;
using AElf.Types;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Shouldly;
using Xunit;

namespace AElf.Contracts.MultiToken
{
    public partial class MultiTokenAuthorizedTest
    {
        [Fact(DisplayName = "[MultiToken] validate the input")]
        public async Task SetSymbolsToPayTxSizeFee_With_Invalid_Input_Test()
        {
            var theDefaultController = await GetDefaultParliamentAddressAsync();
            var primaryTokenSymbol = await GetThePrimaryTokenAsync();
            var FeeToken = "FEETOKEN";
            MainChainTester.ExecuteContractWithMiningAsync(TokenContractAddress,
                nameof(TokenContractImplContainer.TokenContractImplStub.Create), new CreateInput
                {
                    Symbol = FeeToken,
                    TokenName = "name",
                    Issuer = TokenContractAddress,
                    TotalSupply = 100_000
                });
            //primary token weights both should be set to 1
            {
                var newSymbolList = new SymbolListToPayTxSizeFee();
                newSymbolList.SymbolsToPayTxSizeFee.Add(new SymbolToPayTxSizeFee
                {
                    TokenSymbol = primaryTokenSymbol,
                    AddedTokenWeight = 2,
                    BaseTokenWeight = 1
                });
                await VerifyTheInvalidSymbolList(theDefaultController, newSymbolList);
            }

            // include the repeated token.
            {
                var newSymbolList = new SymbolListToPayTxSizeFee
                {
                    SymbolsToPayTxSizeFee =
                    {
                        new SymbolToPayTxSizeFee
                        {
                            TokenSymbol = primaryTokenSymbol,
                            AddedTokenWeight = 1,
                            BaseTokenWeight = 1
                        },
                        new SymbolToPayTxSizeFee
                        {
                            TokenSymbol = FeeToken,
                            AddedTokenWeight = 1,
                            BaseTokenWeight = 1
                        },
                        new SymbolToPayTxSizeFee
                        {
                            TokenSymbol = FeeToken,
                            AddedTokenWeight = 1,
                            BaseTokenWeight = 1
                        }
                    }
                };
                await VerifyTheInvalidSymbolList(theDefaultController, newSymbolList);
            }
            
            // include invalid weright
            {
                {
                    var newSymbolList = new SymbolListToPayTxSizeFee
                    {
                        SymbolsToPayTxSizeFee =
                        {
                            new SymbolToPayTxSizeFee
                            {
                                TokenSymbol = primaryTokenSymbol,
                                AddedTokenWeight = 1,
                                BaseTokenWeight = 1
                            },
                            new SymbolToPayTxSizeFee
                            {
                                TokenSymbol = FeeToken,
                                AddedTokenWeight = 1,
                                BaseTokenWeight = 0
                            }
                        }
                    };
                    await VerifyTheInvalidSymbolList(theDefaultController, newSymbolList);
                }
            }
        }
        private async Task VerifyTheInvalidSymbolList(Address defaultController, SymbolListToPayTxSizeFee newSymbolList)
        {
            var createProposalInput = new CreateProposalInput
            {
                ToAddress = TokenContractAddress,
                Params = newSymbolList.ToByteString(),
                OrganizationAddress = defaultController,
                ContractMethodName = nameof(TokenContractImplContainer.TokenContractImplStub
                    .SetSymbolsToPayTxSizeFee),
                ExpiredTime = TimestampHelper.GetUtcNow().AddHours(1)
            };
            await MainChainTesterCreatApproveAndReleaseProposalForParliamentAsync(createProposalInput);
            var byteResult = await MainChainTester.CallContractMethodAsync(TokenContractAddress,
                nameof(TokenContractImplContainer.TokenContractImplStub.GetSymbolsToPayTxSizeFee),
                new Empty());
            var symbolListToPayTxSizeFee = SymbolListToPayTxSizeFee.Parser.ParseFrom(byteResult);
            symbolListToPayTxSizeFee.SymbolsToPayTxSizeFee.Count.ShouldBe(0);
        }
    }
}