using System.Threading.Tasks;
using AElf.Contracts.MultiToken;
using AElf.Contracts.TestContract.DApp;
using Google.Protobuf.WellKnownTypes;
using Shouldly;
using Xunit;
using Record = AElf.Contracts.TestContract.DApp.Record;

namespace AElf.Contracts.TokenHolder
{
    // ReSharper disable HeuristicUnreachableCode
    public partial class TokenHolderTests
    {
        [Fact]
        public async Task DAppTest()
        {
            await DAppContractStub.SignUp.SendAsync(new Empty());

            (await GetFirstUserBalance("APP")).ShouldBe(10_00000000);
            var elfBalanceBefore = await GetFirstUserBalance("ELF");

            var userTokenStub =
                GetTester<TokenContractContainer.TokenContractStub>(TokenContractAddress, UserKeyPairs[0]);
            await userTokenStub.Approve.SendAsync(new ApproveInput
            {
                Amount = 100_00000000,
                Spender = DAppContractAddress,
                Symbol = "ELF"
            });
            await DAppContractStub.Deposit.SendAsync(new DepositInput
            {
                Amount = 100_00000000
            });

            var elfBalanceAfter = await GetFirstUserBalance("ELF");
            elfBalanceAfter.ShouldBe(elfBalanceBefore - 100_00000000);

            (await GetFirstUserBalance("APP")).ShouldBe(110_00000000);

            var userTokenHolderStub =
                GetTester<TokenHolderContractContainer.TokenHolderContractStub>(TokenHolderContractAddress,
                    UserKeyPairs[0]);
            await userTokenHolderStub.RegisterForProfits.SendAsync(new RegisterForProfitsInput
            {
                SchemeManager = DAppContractAddress,
                Amount = 57_00000000
            });
            for (var i = 0; i < 10; i++)
            {
                await DAppContractStub.Use.SendAsync(new Record());
            }

            (await GetFirstUserBalance("APP")).ShouldBe(50_00000000);

            var receiverTokenStub =
                GetTester<TokenContractContainer.TokenContractStub>(TokenContractAddress, UserKeyPairs[1]);
            await receiverTokenStub.ReceiveProfits.SendAsync(new ReceiveProfitsInput
            {
                ContractAddress = DAppContractAddress,
                Symbol = "ELF",
                Amount = 9000_0000
            });

            await TokenHolderContractStub.ClaimProfits.SendAsync(new ClaimProfitsInput
            {
                Beneficiary = UserAddresses[0],
                SchemeManager = DAppContractAddress,
                Symbol = "ELF"
            });

            (await GetFirstUserBalance("ELF")).ShouldBe(elfBalanceAfter + 1_0000_0000);
        }

        private async Task<long> GetFirstUserBalance(string symbol)
        {
            var balance = await TokenContractStub.GetBalance.CallAsync(new GetBalanceInput
            {
                Owner = UserAddresses[0], Symbol = symbol
            });
            return balance.Balance;
        }
    }
}