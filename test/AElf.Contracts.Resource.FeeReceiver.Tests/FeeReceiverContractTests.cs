using System.Threading.Tasks;
using AElf.Contracts.MultiToken;
using AElf.Contracts.MultiToken.Messages;
using AElf.Contracts.TestBase;
using AElf.Cryptography;
using AElf.Cryptography.ECDSA;
using AElf.Kernel;
using AElf.Kernel.Token;
using AElf.Types;
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

            FeeKeyPair = CryptoHelper.GenerateKeyPair();
            FoundationKeyPair = CryptoHelper.GenerateKeyPair();

            AsyncHelper.RunSync(()=>InitFeeReceiverContract());
        }
        
          #region FeeReceiver Contract cases

        [Fact]
        public async Task Query_FeeReceiver_Information()
        {
            {
                var addressResult = await Tester.CallContractMethodAsync(FeeReceiverContractAddress,
                    nameof(FeeReceiverContractContainer.FeeReceiverContractStub.GetElfTokenAddress), new Empty());
                Address.Parser.ParseFrom(addressResult).ShouldBe(TokenContractAddress);
            }

            {
                var foundationAddress = Tester.GetAddress(FoundationKeyPair);
                var address1Result = await Tester.CallContractMethodAsync(FeeReceiverContractAddress,
                    nameof(FeeReceiverContractContainer.FeeReceiverContractStub.GetFoundationAddress), new Empty());
                Address.Parser.ParseFrom(address1Result).ShouldBe(foundationAddress);
            }
            
            var balanceResult = await Tester.CallContractMethodAsync(FeeReceiverContractAddress,
                nameof(FeeReceiverContractContainer.FeeReceiverContractStub.GetOwedToFoundation), new Empty());
            SInt64Value.Parser.ParseFrom(balanceResult).Value.ShouldBe(0);
        }

        [Fact]
        public async Task FeeReceiver_WithDraw_WithoutPermission()
        {
            var anotherUser = Tester.CreateNewContractTester(CryptoHelper.GenerateKeyPair());
            var withdrawResult = await anotherUser.ExecuteContractWithMiningAsync(FeeReceiverContractAddress,
                nameof(FeeReceiverContractContainer.FeeReceiverContractStub.Withdraw), new SInt32Value {Value = 100});
            withdrawResult.Status.ShouldBe(TransactionResultStatus.Failed);
            withdrawResult.Error.Contains("Only foundation can withdraw token.").ShouldBeTrue();
        }

        [Fact]
        public async Task FeeReceiver_WithDraw_OverToken()
        {
            var founder = Tester.CreateNewContractTester(FoundationKeyPair);
            var withdrawResult = await founder.ExecuteContractWithMiningAsync(FeeReceiverContractAddress,
                nameof(FeeReceiverContractContainer.FeeReceiverContractStub.Withdraw),
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
                nameof(FeeReceiverContractContainer.FeeReceiverContractStub.Withdraw),
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
                nameof(FeeReceiverContractContainer.FeeReceiverContractStub.WithdrawAll),
                new Empty());
            withdrawResult.Status.ShouldBe(TransactionResultStatus.Mined);
        }

        [Fact]
        public async Task FeeReceiver_Burn()
        {
            //Give FeeReceiver address some token for burn operation
            var balance = 5;
            var transferResult = await Tester.ExecuteContractWithMiningAsync(TokenContractAddress,
                nameof(TokenContractContainer.TokenContractStub.Transfer),
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
                nameof(TokenContractContainer.TokenContractStub.GetBalance), new GetBalanceInput
                {
                    Owner = FeeReceiverContractAddress,
                    Symbol = "ELF"
                }));
            var balance1 = feeReceiverBalance.Balance;
            balance1.ShouldBe(balance);

            //Action burn
            var burnResult =
                await Tester.ExecuteContractWithMiningAsync(FeeReceiverContractAddress,
                    nameof(FeeReceiverContractContainer.FeeReceiverContractStub.Burn), new Empty());
            burnResult.Status.ShouldBe(TransactionResultStatus.Mined);

            //Check burned balance.
            feeReceiverBalance = GetBalanceOutput.Parser.ParseFrom(
                await Tester.CallContractMethodAsync(TokenContractAddress,
                nameof(TokenContractContainer.TokenContractStub.GetBalance), new GetBalanceInput
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
                nameof(FeeReceiverContractContainer.FeeReceiverContractStub.Initialize),
                new InitializeInput()
                {
                    ElfTokenAddress = TokenContractAddress,
                    FoundationAddress = foundationAddress
                });
            feeReceiverResult.Status.ShouldBe(TransactionResultStatus.Mined);
        }
    }
}