using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Contracts.MultiToken.Messages;
using AElf.Contracts.TestBase;
using AElf.Cryptography;
using AElf.Kernel;
using AElf.Kernel.Token;
using AElf.OS.Node.Application;
using Newtonsoft.Json.Linq;
using Shouldly;
using Volo.Abp.Threading;
using Xunit;

namespace AElf.Contracts.MultiToken
{
    public class MultiTokenContractWithCustomSystemTransactionTest : ContractTestBase<MultiTokenContractWithCustomSystemTransactionTestAElfModule>
    {
        private Address TokenContractAddress { get; set; }

        private const string SymbolForTestingInitialLogic = "ELFTEST";
        
        public MultiTokenContractWithCustomSystemTransactionTest()
        {
            var keyPair =
                CryptoHelpers.FromPrivateKey(ByteArrayHelpers.FromHexString(TestTokenBalanceContractTestConstants.PrivateKeyHex));
            Tester = new ContractTester<MultiTokenContractWithCustomSystemTransactionTestAElfModule>(1, keyPair);
            var minersKeyPairs = Enumerable.Range(0, 2).Select(_ => CryptoHelpers.GenerateKeyPair())
                .ToList();
            minersKeyPairs.Add(Tester.KeyPair);
            AsyncHelper.RunSync(async () =>
            {
                var tokenContractCallList = new SystemContractDeploymentInput.Types.SystemTransactionMethodCallList();
                tokenContractCallList.Add(nameof(TokenContract.Create), new CreateInput
                {
                    Symbol = SymbolForTestingInitialLogic,
                    Decimals = 2,
                    IsBurnable = true,
                    Issuer = Tester.GetCallOwnerAddress(),
                    TokenName = "elf token",
                    TotalSupply = Tester.TokenTotalSupply
                });

                // For testing.
                tokenContractCallList.Add(nameof(TokenContract.Issue), new IssueInput
                {
                    Symbol = SymbolForTestingInitialLogic,
                    Amount = Tester.TokenTotalSupply,
                    To = Tester.GetCallOwnerAddress(),
                    Memo = "Issue token to starter himself."
                });

                await Tester.InitialCustomizedChainAsync(minersKeyPairs.Select(m => m.PublicKey.ToHex()).ToList(),
                    4000, null,
                    list =>
                    {
                        list.AddGenesisSmartContract<TokenContract>(TokenSmartContractAddressNameProvider.Name,
                            tokenContractCallList);
                    });
            });
            TokenContractAddress = Tester.GetContractAddress(TokenSmartContractAddressNameProvider.Name);
        }
        
        [Fact]
        public async Task TokenContract_WithSystemTransaction()
        {
            var getBalanceTx = await Tester.GenerateTransactionAsync(TokenContractAddress,
                nameof(TokenContract.GetBalance),
                new GetBalanceInput
                {
                    Symbol = SymbolForTestingInitialLogic,
                    Owner = Tester.GetCallOwnerAddress()
                });
            var block = await Tester.MineAsync(new List<Transaction> {getBalanceTx});
            var transactionResults = new List<TransactionResult>();
            foreach (var transactionHash in block.Body.Transactions)
            {
                var transactionResult =  await Tester.GetTransactionResultAsync(transactionHash);
                transactionResults.Add(transactionResult);
            }
            
            var ownerBalance = GetBalanceOutput.Parser.ParseFrom(
                await Tester.CallContractMethodAsync(TokenContractAddress, nameof(TokenContract.GetBalance),
                    new GetBalanceInput
                    {
                        Symbol = SymbolForTestingInitialLogic,
                        Owner = Tester.GetCallOwnerAddress()
                    })).Balance;
            
            ownerBalance.ShouldBe(Tester.TokenTotalSupply - 1000L);

            foreach (var transactionResult in transactionResults)
            {
                var returnValue = JObject.Parse(transactionResult.ReadableReturnValue);
                returnValue["balance"]?.Value<long>().ShouldBe(Tester.TokenTotalSupply - 1000L);
            }
        }
    }
}