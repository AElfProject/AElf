using System.Threading.Tasks;
using AElf.Common;
using AElf.Contracts.MultiToken;
using AElf.Contracts.MultiToken.Messages;
using AElf.Contracts.TestBase;
using AElf.Cryptography;
using AElf.Cryptography.ECDSA;
using AElf.Kernel;
using AElf.Kernel.Token;
using Google.Protobuf.WellKnownTypes;
using Shouldly;
using Volo.Abp.Threading;
using Xunit;

namespace AElf.Contracts.Resource.FeeReceiver
{
    public class FeeReceiverContractTests : ContractTestBase<FeeReceiverContractTestAElfModule>
    {
        private ECKeyPair FeeKeyPair;
        private ECKeyPair FoundationKeyPair; 
        
        private Address TokenContractAddress;
        private Address FeeReceiverContractAddress;

        public FeeReceiverContractTests()
        {
            AsyncHelper.RunSync(() => Tester.InitialChainAndTokenAsync());
            TokenContractAddress = Tester.GetContractAddress(TokenSmartContractAddressNameProvider.Name);
            FeeReceiverContractAddress =
                Tester.GetContractAddress(ResourceFeeReceiverSmartContractAddressNameProvider.Name);

            FeeKeyPair = CryptoHelpers.GenerateKeyPair();
            FoundationKeyPair = CryptoHelpers.GenerateKeyPair();

            AsyncHelper.RunSync(()=>InitFeeReceiverContract());
        }
        
          #region FeeReceiver Contract cases

        [Fact]
        public async Task Query_FeeReceiver_Information()
        {
            {
                var addressResult = await Tester.CallContractMethodAsync(FeeReceiverContractAddress,
                    nameof(FeeReceiverContract.GetElfTokenAddress), new Empty());
                Address.Parser.ParseFrom(addressResult).ShouldBe(TokenContractAddress);
            }

            {
                var foundationAddress = Tester.GetAddress(FoundationKeyPair);
                var address1Result = await Tester.CallContractMethodAsync(FeeReceiverContractAddress,
                    nameof(FeeReceiverContract.GetFoundationAddress), new Empty());
                Address.Parser.ParseFrom(address1Result).ShouldBe(foundationAddress);
            }
            
            var balanceResult = await Tester.CallContractMethodAsync(FeeReceiverContractAddress,
                nameof(FeeReceiverContract.GetOwedToFoundation), new Empty());
            SInt64Value.Parser.ParseFrom(balanceResult).Value.ShouldBe(0);
        }

        [Fact]
        public async Task FeeReceiver_WithDraw_WithoutPermission()
        {
            var anotherUser = Tester.CreateNewContractTester(CryptoHelpers.GenerateKeyPair());
            var withdrawResult = await anotherUser.ExecuteContractWithMiningAsync(FeeReceiverContractAddress,
                nameof(FeeReceiverContract.Withdraw), new SInt32Value {Value = 100});
            withdrawResult.Status.ShouldBe(TransactionResultStatus.Failed);
            withdrawResult.Error.Contains("Only foundation can withdraw token.").ShouldBeTrue();
        }

        [Fact]
        public async Task FeeReceiver_WithDraw_OverToken()
        {
            var founder = Tester.CreateNewContractTester(FoundationKeyPair);
            var withdrawResult = await founder.ExecuteContractWithMiningAsync(FeeReceiverContractAddress,
                nameof(FeeReceiverContract.Withdraw),
                new SInt64Value()
                {
                    Value = 100
                });
            withdrawResult.Status.ShouldBe(TransactionResultStatus.Failed);
            withdrawResult.Error.Contains("Too much to withdraw.").ShouldBeTrue();
        }

        [Fact]
        public async Task FeeReceiver_WithDraw_NormalCase()
        {
            var founder = Tester.CreateNewContractTester(FoundationKeyPair);
            var withdrawResult = await founder.ExecuteContractWithMiningAsync(FeeReceiverContractAddress,
                nameof(FeeReceiverContract.Withdraw),
                new SInt64Value()
                {
                    Value = 0
                });
            withdrawResult.Status.ShouldBe(TransactionResultStatus.Mined);
        }

        [Fact]
        public async Task FeeReceiver_WithDraw_all()
        {
            var founder = Tester.CreateNewContractTester(FoundationKeyPair);
            var withdrawResult = await founder.ExecuteContractWithMiningAsync(FeeReceiverContractAddress,
                nameof(FeeReceiverContract.WithdrawAll),
                new Empty());
            withdrawResult.Status.ShouldBe(TransactionResultStatus.Mined);
        }

        [Fact]
        public async Task FeeReceiver_Burn()
        {
            //Give FeeReceiver address some token for burn operation
            var balance = 5;
            var transferResult = await Tester.ExecuteContractWithMiningAsync(TokenContractAddress,
                nameof(TokenContract.Transfer),
                new TransferInput()
                {
                    Symbol = "ELF",
                    To = FeeReceiverContractAddress,
                    Amount = balance,
                    Memo = "Just for burn test"
                });
            transferResult.Status.ShouldBe(TransactionResultStatus.Mined);

            //Check balance before burn
            var feeReceiverBalance =GetBalanceOutput.Parser.ParseFrom(
                await Tester.CallContractMethodAsync(TokenContractAddress,
                nameof(TokenContract.GetBalance), new GetBalanceInput
                {
                    Owner = FeeReceiverContractAddress,
                    Symbol = "ELF"
                }));
            var balance1 = feeReceiverBalance.Balance;
            balance1.ShouldBe(balance);

            //Action burn
            var burnResult =
                await Tester.ExecuteContractWithMiningAsync(FeeReceiverContractAddress,
                    nameof(FeeReceiverContract.Burn), new Empty());
            burnResult.Status.ShouldBe(TransactionResultStatus.Mined);

            //Check burned balance.
            feeReceiverBalance = GetBalanceOutput.Parser.ParseFrom(
                await Tester.CallContractMethodAsync(TokenContractAddress,
                nameof(TokenContract.GetBalance), new GetBalanceInput
                {
                    Owner = FeeReceiverContractAddress,
                    Symbol = "ELF"
                }));
            var balance2 = feeReceiverBalance.Balance;
            balance2.ShouldBeLessThan(balance1);
        }

        #endregion

        private async Task InitFeeReceiverContract()
        {
            //init fee receiver contract
            var foundationAddress = Tester.GetAddress(FoundationKeyPair);
            var feeReceiverResult = await Tester.ExecuteContractWithMiningAsync(FeeReceiverContractAddress,
                nameof(FeeReceiverContract.Initialize),
                new InitializeInput()
                {
                    ElfTokenAddress = TokenContractAddress,
                    FoundationAddress = foundationAddress
                });
            feeReceiverResult.Status.ShouldBe(TransactionResultStatus.Mined);
        }
    }
}