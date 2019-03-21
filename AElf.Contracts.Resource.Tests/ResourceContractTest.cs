using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using AElf.Common;
using AElf.Contracts.Resource.FeeReceiver;
using AElf.Contracts.TestBase;
using AElf.Contracts.MultiToken;
using AElf.Contracts.MultiToken.Messages;
using AElf.Cryptography;
using AElf.Cryptography.ECDSA;
using AElf.Kernel;
using AElf.Kernel.KernelAccount;
using AElf.Kernel.SmartContract;
using AElf.Kernel.Token;
using AElf.Types.CSharp;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Xunit;
using Shouldly;
using Volo.Abp.Threading;

namespace AElf.Contracts.Resource.Tests
{
    public class ResourceContractTest : ContractTestBase<ResourceContractTestAElfModule>
    {
        private ECKeyPair FeeKeyPair;
        private ECKeyPair FoundationKeyPair;

        private Address BasicZeroContractAddress;
        private Address TokenContractAddress;
        private Address ResourceContractAddress;
        private Address FeeReceiverContractAddress;

        public ResourceContractTest()
        {
            AsyncHelper.RunSync(() => Tester.InitialChainAndTokenAsync());

            BasicZeroContractAddress = Tester.GetZeroContractAddress();
            TokenContractAddress = Tester.GetContractAddress(TokenSmartContractAddressNameProvider.Name);
            ResourceContractAddress = Tester.GetContractAddress(ResourceSmartContractAddressNameProvider.Name);
            FeeReceiverContractAddress =
                Tester.GetContractAddress(ResourceFeeReceiverSmartContractAddressNameProvider.Name);

            FeeKeyPair = CryptoHelpers.GenerateKeyPair();
            FoundationKeyPair = CryptoHelpers.GenerateKeyPair();
        }

        [Fact]
        public async Task Deploy_Contracts()
        {
            var tokenTx = await Tester.GenerateTransactionAsync(BasicZeroContractAddress,
                nameof(ISmartContractZero.DeploySmartContract), 2,
                File.ReadAllBytes(typeof(TokenContract).Assembly.Location));
            var resourceTx = await Tester.GenerateTransactionAsync(BasicZeroContractAddress,
                nameof(ISmartContractZero.DeploySmartContract), 2,
                File.ReadAllBytes(typeof(ResourceContract).Assembly.Location));

            await Tester.MineAsync(new List<Transaction> {tokenTx, resourceTx});
            var chain = await Tester.GetChainAsync();
            chain.LongestChainHeight.ShouldBeGreaterThanOrEqualTo(1);
        }

        [Fact]
        public async Task Initialize_Resource()
        {
            //init fee receiver contract
            var foundationAddress = Tester.GetAddress(FoundationKeyPair);
            var feeReceiverResult = await Tester.ExecuteContractWithMiningAsync(FeeReceiverContractAddress,
                nameof(FeeReceiverContract.Initialize),
                TokenContractAddress, foundationAddress);
            feeReceiverResult.Status.ShouldBe(TransactionResultStatus.Mined);

            //init resource contract
            var feeAddress = Tester.GetAddress(FeeKeyPair);
            var resourceResult = await Tester.ExecuteContractWithMiningAsync(ResourceContractAddress,
                nameof(ResourceContract.Initialize),
                TokenContractAddress, feeAddress, feeAddress);
            resourceResult.Status.ShouldBe(TransactionResultStatus.Mined);
        }

        #region FeeReceiver Contract cases

        [Fact]
        public async Task Query_FeeReceiver_Information()
        {
            await Initialize_Resource();

            var addressResult = await Tester.CallContractMethodAsync(FeeReceiverContractAddress,
                nameof(FeeReceiverContract.GetElfTokenAddress));
            addressResult.DeserializeToString().ShouldBe(TokenContractAddress.GetFormatted());

            var foundationAddress = Tester.GetAddress(FoundationKeyPair).GetFormatted();
            var address1Result = await Tester.CallContractMethodAsync(FeeReceiverContractAddress,
                nameof(FeeReceiverContract.GetFoundationAddress));
            address1Result.DeserializeToString().ShouldBe(foundationAddress);

            var balanceResult = await Tester.CallContractMethodAsync(FeeReceiverContractAddress,
                nameof(FeeReceiverContract.GetOwedToFoundation));
            balanceResult.DeserializeToInt64().ShouldBe(0);
        }

        [Fact]
        public async Task FeeReceiver_WithDraw_WithoutPermission()
        {
            await Initialize_Resource();

            var anotherUser = Tester.CreateNewContractTester(CryptoHelpers.GenerateKeyPair());
            var withdrawResult = await anotherUser.ExecuteContractWithMiningAsync(FeeReceiverContractAddress,
                nameof(FeeReceiverContract.Withdraw),
                100);
            withdrawResult.Status.ShouldBe(TransactionResultStatus.Failed);
            withdrawResult.Error.Contains("Only foundation can withdraw token.").ShouldBeTrue();
        }

        [Fact]
        public async Task FeeReceiver_WithDraw_OverToken()
        {
            await Initialize_Resource();

            var founder = Tester.CreateNewContractTester(FoundationKeyPair);
            var withdrawResult = await founder.ExecuteContractWithMiningAsync(FeeReceiverContractAddress,
                nameof(FeeReceiverContract.Withdraw),
                100);
            withdrawResult.Status.ShouldBe(TransactionResultStatus.Failed);
            withdrawResult.Error.Contains("Too much to withdraw.").ShouldBeTrue();
        }

        [Fact]
        public async Task FeeReceiver_WithDraw_NormalCase()
        {
            await Initialize_Resource();

            var founder = Tester.CreateNewContractTester(FoundationKeyPair);
            var withdrawResult = await founder.ExecuteContractWithMiningAsync(FeeReceiverContractAddress,
                nameof(FeeReceiverContract.Withdraw),
                0);
            withdrawResult.Status.ShouldBe(TransactionResultStatus.Mined);
        }

        [Fact]
        public async Task FeeReceiver_WithDraw_all()
        {
            await Initialize_Resource();

            var founder = Tester.CreateNewContractTester(FoundationKeyPair);
            var withdrawResult = await founder.ExecuteContractWithMiningAsync(FeeReceiverContractAddress,
                nameof(FeeReceiverContract.WithdrawAll));
            withdrawResult.Status.ShouldBe(TransactionResultStatus.Mined);
        }

        [Fact(Skip = "https://github.com/AElfProject/AElf/issues/1227")]
        public async Task FeeReceiver_Burn()
        {
            await Initialize_Resource();

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
            var feeReceiverBalance = await Tester.CallContractMethodAsync(TokenContractAddress,
                nameof(TokenContract.GetBalance), new GetBalanceInput
                {
                    Owner = FeeReceiverContractAddress,
                    Symbol = "ELF"
                });
            var balance1 = feeReceiverBalance.DeserializeToPbMessage<GetBalanceOutput>().Balance;
            balance1.ShouldBe(balance);

            //Action burn
            var burnResult =
                await Tester.ExecuteContractWithMiningAsync(FeeReceiverContractAddress,
                    nameof(FeeReceiverContract.Burn));
            burnResult.Status.ShouldBe(TransactionResultStatus.Mined);

            //Check burned balance.
            feeReceiverBalance = await Tester.CallContractMethodAsync(TokenContractAddress,
                nameof(TokenContract.GetBalance), new GetBalanceInput
                {
                    Owner = FeeReceiverContractAddress,
                    Symbol = "ELF"
                });
            var balance2 = feeReceiverBalance.DeserializeToPbMessage<GetBalanceOutput>().Balance;
            balance2.ShouldBeLessThan(balance1);
        }

        #endregion

        #region Resource Contract cases

        [Fact]
        public async Task Query_Resource_AddressInfo()
        {
            await Initialize_Resource();

            //verify result
            var tokenAddress = await Tester.CallContractMethodAsync(ResourceContractAddress,
                nameof(ResourceContract.GetElfTokenAddress));
            tokenAddress.DeserializeToString().ShouldBe(TokenContractAddress.GetFormatted());

            var address = Tester.GetAddress(FeeKeyPair);
            var feeAddressString = address.GetFormatted();
            var feeAddress =
                await Tester.CallContractMethodAsync(ResourceContractAddress, nameof(ResourceContract.GetFeeAddress));
            feeAddress.DeserializeToString().ShouldBe(feeAddressString);

            var controllerAddress = await Tester.CallContractMethodAsync(ResourceContractAddress,
                nameof(ResourceContract.GetResourceControllerAddress));
            controllerAddress.DeserializeToString().ShouldBe(feeAddressString);
        }

        [Fact]
        public async Task Query_Resource_ConverterInfo()
        {
            await Initialize_Resource();

            var cpuConverter = await Tester.CallContractMethodAsync(ResourceContractAddress,
                nameof(ResourceContract.GetConverter), "Cpu");
            var cpuString = cpuConverter.DeserializeToString();
            var cpuObj = JsonConvert.DeserializeObject<JObject>(cpuString);
            cpuObj["ResBalance"].ToObject<long>().ShouldBe(1000_000L);
            cpuObj["ResWeight"].ToObject<long>().ShouldBe(500_000L);
            cpuObj["ResourceType"].ToObject<string>().ShouldBe("Cpu");
        }

        [Fact]
        public async Task Query_Exchange_Balance()
        {
            await Initialize_Resource();

            var exchangeResult = await Tester.CallContractMethodAsync(ResourceContractAddress,
                nameof(ResourceContract.GetExchangeBalance), "Cpu");
            exchangeResult.DeserializeToInt64().ShouldBe(1000_000L);
        }

        [Fact]
        public async Task Query_Elf_Balance()
        {
            await Initialize_Resource();

            var elfResult = await Tester.CallContractMethodAsync(ResourceContractAddress,
                nameof(ResourceContract.GetElfBalance), "Cpu");
            elfResult.DeserializeToInt64().ShouldBe(1000_000L);
        }

        [Fact]
        public async Task IssueResource_With_Controller_Account()
        {
            await Initialize_Resource();

            var receiver = Tester.CreateNewContractTester(FeeKeyPair);
            var issueResult = await receiver.ExecuteContractWithMiningAsync(ResourceContractAddress,
                nameof(ResourceContract.IssueResource),
                "Cpu", 100_000L);

            issueResult.Status.ShouldBe(TransactionResultStatus.Mined);

            //check result
            var cpuConverter = await receiver.CallContractMethodAsync(ResourceContractAddress,
                nameof(ResourceContract.GetConverter), "Cpu");
            var cpuString = cpuConverter.DeserializeToString();
            var cpuObj = JsonConvert.DeserializeObject<JObject>(cpuString);
            cpuObj["ResBalance"].ToObject<long>().ShouldBe(1000_000L + 100_000L);
        }

        [Fact]
        public async Task IssueResource_WithNot_Controller_Account()
        {
            await Initialize_Resource();

            var otherKeyPair = Tester.KeyPair;
            var issueResult = await Tester.ExecuteContractWithMiningAsync(ResourceContractAddress,
                nameof(ResourceContract.IssueResource),
                "CPU", 100_000L);
            issueResult.Status.ShouldBe(TransactionResultStatus.Failed);
            issueResult.Error.Contains("Only resource controller is allowed to perform this action.").ShouldBe(true);
        }

        [Theory]
        [InlineData(10L)]
        [InlineData(100L)]
        [InlineData(1000L)]
        [InlineData(10000L)]
        public async Task Buy_Resource_WithEnough_Token(long paidElf)
        {
            await Initialize_Resource();
            var ownerAddress = Tester.GetAddress(Tester.KeyPair);

            //Approve first
            await ApproveBalance(paidElf);

            //Buy resource
            var buyResult = await Tester.ExecuteContractWithMiningAsync(ResourceContractAddress,
                nameof(ResourceContract.BuyResource),
                "Cpu", paidElf);
            var returnMessage = buyResult.ReturnValue.ToStringUtf8();
            returnMessage.ShouldBe(string.Empty);
            buyResult.Status.ShouldBe(TransactionResultStatus.Mined);

            //Check result
            var tokenBalance =
                await Tester.CallContractMethodAsync(TokenContractAddress, nameof(TokenContract.GetBalance),
                    new GetBalanceInput
                    {
                        Owner = ownerAddress,
                        Symbol = "ELF"
                    });
            tokenBalance.DeserializeToPbMessage<GetBalanceOutput>().Balance.ShouldBe(1000_000L - paidElf);

            var cpuBalance = await Tester.CallContractMethodAsync(ResourceContractAddress,
                nameof(ResourceContract.GetUserBalance),
                ownerAddress, "Cpu");
            cpuBalance.DeserializeToInt64().ShouldBeGreaterThan(0L);
        }

        [Fact]
        public async Task Buy_Resource_WithoutEnough_Token()
        {
            await Initialize_Resource();

            var noTokenKeyPair = Tester.KeyPair;
            var buyResult = await Tester.ExecuteContractWithMiningAsync(ResourceContractAddress,
                nameof(ResourceContract.BuyResource),
                "Cpu", 10_000L);
            buyResult.Status.ShouldBe(TransactionResultStatus.Failed);
            buyResult.Error.Contains("Insufficient allowance.").ShouldBeTrue();
        }

        [Fact]
        public async Task Buy_NotExist_Resource()
        {
            await Initialize_Resource();

            //Buy resource
            var buyResult = await Tester.ExecuteContractWithMiningAsync(ResourceContractAddress,
                nameof(ResourceContract.BuyResource),
                "TestResource", 100L);
            buyResult.Status.ShouldBe(TransactionResultStatus.Failed);
            buyResult.Error.Contains("Incorrect resource type.").ShouldBeTrue();
        }

        [Fact]
        public async Task Sell_WithEnough_Resource()
        {
            await Buy_Resource_WithEnough_Token(1000L);

            var sellResult = await Tester.ExecuteContractWithMiningAsync(ResourceContractAddress,
                nameof(ResourceContract.SellResource),
                "Cpu", 100L);
            sellResult.Status.ShouldBe(TransactionResultStatus.Mined);
        }

        [Fact]
        public async Task Sell_WithoutEnough_Resource()
        {
            await Buy_Resource_WithEnough_Token(100L);

            var sellResult = await Tester.ExecuteContractWithMiningAsync(ResourceContractAddress,
                nameof(ResourceContract.SellResource),
                "Cpu", 1000L);
            sellResult.Status.ShouldBe(TransactionResultStatus.Failed);
            sellResult.Error.Contains("Insufficient CPU balance.").ShouldBe(true);
        }

        [Fact]
        public async Task Sell_NotExist_Resource()
        {
            await Initialize_Resource();

            var sellResult = await Tester.ExecuteContractWithMiningAsync(ResourceContractAddress,
                nameof(ResourceContract.SellResource),
                "TestResource", 100L);
            sellResult.Status.ShouldBe(TransactionResultStatus.Failed);
            sellResult.Error.Contains("Incorrect resource type.").ShouldBeTrue();
        }

        [Fact]
        public async Task Lock_Available_Resource()
        {
            await Buy_Resource_WithEnough_Token(1000L);

            var ownerAddress = Tester.GetAddress(Tester.KeyPair);
            var resourceResult = await Tester.CallContractMethodAsync(ResourceContractAddress,
                nameof(ResourceContract.GetUserBalance),
                ownerAddress, "Cpu");
            var resourceBalance1 = resourceResult.DeserializeToInt64();

            //Action
            var lockResult = await Tester.ExecuteContractWithMiningAsync(ResourceContractAddress,
                nameof(ResourceContract.LockResource),
                100L, "Cpu");
            lockResult.Status.ShouldBe(TransactionResultStatus.Mined);

            //Verify
            resourceResult = await Tester.CallContractMethodAsync(ResourceContractAddress,
                nameof(ResourceContract.GetUserBalance),
                ownerAddress, "Cpu");
            var resourceBalance2 = resourceResult.DeserializeToInt64();
            resourceBalance1.ShouldBe(resourceBalance2 + 100L);

            var lockedResult = await Tester.CallContractMethodAsync(ResourceContractAddress,
                nameof(ResourceContract.GetUserLockedBalance),
                ownerAddress, "Cpu");
            var lockedBalance = lockedResult.DeserializeToInt64();
            lockedBalance.ShouldBe(100L);

            var controllerAddress = Tester.GetAddress(FeeKeyPair);
            var controllerResourceResult = await Tester.CallContractMethodAsync(ResourceContractAddress,
                nameof(ResourceContract.GetUserBalance),
                controllerAddress, "Cpu");
            var controllerBalance = controllerResourceResult.DeserializeToInt64();
            controllerBalance.ShouldBe(100L);
        }

        [Fact(Skip = "long type won't throw exception, maybe need another way to test.")]
        public async Task Lock_OverOwn_Resource()
        {
            await Initialize_Resource();

            var lockResult = await Tester.ExecuteContractWithMiningAsync(ResourceContractAddress,
                nameof(ResourceContract.LockResource),
                1000L, "Cpu");
            lockResult.Status.ShouldBe(TransactionResultStatus.Failed);
            lockResult.Error.Contains("System.OverflowException: Arithmetic operation resulted in an overflow.")
                .ShouldBe(true);
        }

        [Fact]
        public async Task Unlock_Available_Resource()
        {
            await Buy_Resource_WithEnough_Token(1000L);
            var ownerAddress = Tester.GetAddress(Tester.KeyPair);
            var resourceResult = await Tester.CallContractMethodAsync(ResourceContractAddress,
                nameof(ResourceContract.GetUserBalance),
                ownerAddress, "Cpu");
            var userBalance0 = resourceResult.DeserializeToInt64();

            //Action
            var lockResult = await Tester.ExecuteContractWithMiningAsync(ResourceContractAddress,
                nameof(ResourceContract.LockResource),
                100L, "Cpu");
            lockResult.Status.ShouldBe(TransactionResultStatus.Mined);

            var controllerAddress = Tester.GetAddress(FeeKeyPair);
            var receiver = Tester.CreateNewContractTester(FeeKeyPair);
            var unlockResult = await receiver.ExecuteContractWithMiningAsync(ResourceContractAddress,
                nameof(ResourceContract.UnlockResource),
                ownerAddress, 50L, "Cpu");
            unlockResult.Status.ShouldBe(TransactionResultStatus.Mined);

            //Verify
            resourceResult = await receiver.CallContractMethodAsync(ResourceContractAddress,
                nameof(ResourceContract.GetUserBalance),
                ownerAddress, "Cpu");
            var userBalance1 = resourceResult.DeserializeToInt64();
            userBalance0.ShouldBe(userBalance1 + 50L);

            var resource1Result = await receiver.CallContractMethodAsync(ResourceContractAddress,
                nameof(ResourceContract.GetUserBalance),
                controllerAddress, "Cpu");
            var controllerBalance = resource1Result.DeserializeToInt64();
            controllerBalance.ShouldBe(50L);

            var lockedResult = await receiver.CallContractMethodAsync(ResourceContractAddress,
                nameof(ResourceContract.GetUserLockedBalance),
                ownerAddress, "Cpu");
            var lockedBalance = lockedResult.DeserializeToInt64();
            lockedBalance.ShouldBe(50L);
        }

        [Fact]
        public async Task Unlock_WithNot_Controller()
        {
            await Buy_Resource_WithEnough_Token(1000L);
            var ownerAddress = Tester.GetAddress(Tester.KeyPair);

            //Action
            var lockResult = await Tester.ExecuteContractWithMiningAsync(ResourceContractAddress,
                nameof(ResourceContract.LockResource),
                100L, "Cpu");
            lockResult.Status.ShouldBe(TransactionResultStatus.Mined);

            var unlockResult = await Tester.ExecuteContractWithMiningAsync(ResourceContractAddress,
                nameof(ResourceContract.UnlockResource),
                ownerAddress, 50L, "Cpu");
            unlockResult.Status.ShouldBe(TransactionResultStatus.Failed);
            unlockResult.Error.Contains("Only the resource controller can perform this action.").ShouldBeTrue();
        }

        [Fact(Skip = "long type won't throw exception, maybe need another way to test.")]
        public async Task Unlock_OverLocked_Resource()
        {
            await Buy_Resource_WithEnough_Token(1000L);
            var ownerAddress = Tester.GetAddress(Tester.KeyPair);

            //Action
            var lockResult = await Tester.ExecuteContractWithMiningAsync(ResourceContractAddress,
                nameof(ResourceContract.LockResource),
                100L, "Cpu");
            lockResult.Status.ShouldBe(TransactionResultStatus.Mined);

            var receiver = Tester.CreateNewContractTester(FeeKeyPair);
            var unlockResult = await receiver.ExecuteContractWithMiningAsync(ResourceContractAddress,
                nameof(ResourceContract.UnlockResource),
                ownerAddress, 200L, "Cpu");
            unlockResult.Status.ShouldBe(TransactionResultStatus.Failed);
            unlockResult.Error.Contains("Arithmetic operation resulted in an overflow.").ShouldBeTrue();
        }

        private async Task ApproveBalance(long amount)
        {
            var callOwner = Tester.GetAddress(Tester.KeyPair);

            var resourceResult = await Tester.ExecuteContractWithMiningAsync(TokenContractAddress,
                nameof(TokenContract.Approve), new ApproveInput
                {
                    Spender = ResourceContractAddress,
                    Symbol = "ELF",
                    Amount = amount
                });
            resourceResult.Status.ShouldBe(TransactionResultStatus.Mined);
            var allowanceResult1 = await Tester.CallContractMethodAsync(TokenContractAddress,
                nameof(TokenContract.GetAllowance), new GetAllowanceInput
                {
                    Owner = callOwner,
                    Spender = ResourceContractAddress,
                    Symbol = "ELF"
                });
            Console.WriteLine(
                $"Allowance Query: {ResourceContractAddress} = {allowanceResult1.DeserializeToPbMessage<GetAllowanceOutput>().Allowance}");
        }

        #endregion
    }
}