using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using AElf.Common;
using AElf.Contracts.Genesis;
using AElf.Contracts.Resource.FeeReceiver;
using AElf.Contracts.TestBase;
using AElf.Contracts.Token;
using AElf.Cryptography;
using AElf.Cryptography.ECDSA;
using AElf.Kernel;
using AElf.Kernel.KernelAccount;
using AElf.Kernel.Types.SmartContract;
using AElf.Types.CSharp;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Xunit;
using Shouldly;
using Volo.Abp.Threading;

namespace AElf.Contracts.Resource.Tests
{
    public class ResourceContractTest: ResourceContractTestBase
    {
        private ContractTester Tester;
        private ECKeyPair FeeKeyPair;
        private ECKeyPair FoundationKeyPair;

        private Address BasicZeroContractAddress;
        private Address TokenContractAddress;
        private Address ResourceContractAddress;
        private Address FeeReceiverContractAddress;

        public ResourceContractTest()
        {
            Tester = new ContractTester();
            var contractArray = Tester.GetDefaultContractTypes();
            contractArray.Add(typeof(FeeReceiverContract));
            AsyncHelper.RunSync(() => Tester.InitialChainAsync(contractArray.ToArray()));

            BasicZeroContractAddress = Tester.GetZeroContractAddress();
            TokenContractAddress = Tester.GetContractAddress(typeof(TokenContract));
            ResourceContractAddress = Tester.GetContractAddress(typeof(ResourceContract));
            FeeReceiverContractAddress = Tester.GetContractAddress(typeof(FeeReceiverContract));

            FeeKeyPair = CryptoHelpers.GenerateKeyPair();
            FoundationKeyPair = CryptoHelpers.GenerateKeyPair();
        }

        [Fact]
        public async Task Deploy_Contracts()
        {
            var tokenTx = Tester.GenerateTransaction(BasicZeroContractAddress, nameof(ISmartContractZero.DeploySmartContract), 2,
                File.ReadAllBytes(typeof(TokenContract).Assembly.Location));
            var resourceTx = Tester.GenerateTransaction(BasicZeroContractAddress, nameof(ISmartContractZero.DeploySmartContract), 2,
                File.ReadAllBytes(typeof(ResourceContract).Assembly.Location));

            await Tester.MineABlockAsync(new List<Transaction> {tokenTx, resourceTx});
            var chain = await Tester.GetChainAsync();
            chain.LongestChainHeight.ShouldBeGreaterThanOrEqualTo(1);
        }

        [Fact]
        public async Task Initialize_Resource()
        {
            //init token contract
            var initResult = await Tester.ExecuteContractWithMiningAsync(TokenContractAddress, nameof(TokenContract.Initialize),
                "ELF", "elf token", 1000_000UL, 2U);
            initResult.Status.ShouldBe(TransactionResultStatus.Mined);

            //init fee receiver contract
            var foundationAddress = Tester.GetAddress(FoundationKeyPair);
            var feeReceiverResult = await Tester.ExecuteContractWithMiningAsync(FeeReceiverContractAddress, nameof(FeeReceiverContract.Initialize),
                TokenContractAddress, foundationAddress);
            feeReceiverResult.Status.ShouldBe(TransactionResultStatus.Mined);

            //init resource contract
            var feeAddress = Tester.GetAddress(FeeKeyPair);
            var resourceResult = await Tester.ExecuteContractWithMiningAsync(ResourceContractAddress, nameof(ResourceContract.Initialize),
                TokenContractAddress, feeAddress, feeAddress);
            resourceResult.Status.ShouldBe(TransactionResultStatus.Mined);
        }

        #region FeeReceiver Contract cases

        [Fact]
        public async Task Query_FeeReceiver_Information()
        {
            await Initialize_Resource();

            var addressResult = await Tester.CallContractMethodAsync(FeeReceiverContractAddress, nameof(FeeReceiverContract.GetElfTokenAddress));
            addressResult.DeserializeToString().ShouldBe(TokenContractAddress.GetFormatted());

            var foundationAddress = Tester.GetAddress(FoundationKeyPair).GetFormatted();
            var address1Result = await Tester.CallContractMethodAsync(FeeReceiverContractAddress, nameof(FeeReceiverContract.GetFoundationAddress));
            address1Result.DeserializeToString().ShouldBe(foundationAddress);

            var balanceResult = await Tester.CallContractMethodAsync(FeeReceiverContractAddress, nameof(FeeReceiverContract.GetOwedToFoundation));
            balanceResult.DeserializeToUInt64().ShouldBe(0u);
        }

        [Fact]
        public async Task FeeReceiver_WithDraw_WithoutPermission()
        {
            await Initialize_Resource();

            Tester.SetCallOwner(CryptoHelpers.GenerateKeyPair());
            var withdrawResult = await Tester.ExecuteContractWithMiningAsync(FeeReceiverContractAddress, nameof(FeeReceiverContract.Withdraw),
                100);
            withdrawResult.Status.ShouldBe(TransactionResultStatus.Failed);
            withdrawResult.Error.Contains("Only foundation can withdraw token.").ShouldBeTrue();
        }

        [Fact]
        public async Task FeeReceiver_WithDraw_OverToken()
        {
            await Initialize_Resource();

            Tester.SetCallOwner(FoundationKeyPair);
            var withdrawResult = await Tester.ExecuteContractWithMiningAsync(FeeReceiverContractAddress, nameof(FeeReceiverContract.Withdraw),
                100);
            withdrawResult.Status.ShouldBe(TransactionResultStatus.Failed);
            withdrawResult.Error.Contains("Too much to withdraw.").ShouldBeTrue();
        }

        [Fact]
        public async Task FeeReceiver_WithDraw_NormalCase()
        {
            await Initialize_Resource();

            Tester.SetCallOwner(FoundationKeyPair);
            var withdrawResult = await Tester.ExecuteContractWithMiningAsync(FeeReceiverContractAddress, nameof(FeeReceiverContract.Withdraw),
                0);
            withdrawResult.Status.ShouldBe(TransactionResultStatus.Mined);
        }

        [Fact]
        public async Task FeeReceiver_WithDraw_all()
        {
            await Initialize_Resource();

            Tester.SetCallOwner(FoundationKeyPair);
            var withdrawResult = await Tester.ExecuteContractWithMiningAsync(FeeReceiverContractAddress, nameof(FeeReceiverContract.WithdrawAll));
            withdrawResult.Status.ShouldBe(TransactionResultStatus.Mined);
        }

        [Fact(Skip="Not implement issuue.")]
        public async Task FeeReceiver_Burn()
        {
            await Initialize_Resource();

            var burnResult = await Tester.ExecuteContractWithMiningAsync(FeeReceiverContractAddress, "Burn");
            var returnMessage = burnResult.ReturnValue.ToStringUtf8();
            burnResult.Status.ShouldBe(TransactionResultStatus.Mined);
        }

        #endregion

        #region Resource Contract cases

        [Fact]
        public async Task Query_Resource_AddressInfo()
        {
            await Initialize_Resource();

            //verify result
            var tokenAddress = await Tester.CallContractMethodAsync(ResourceContractAddress, nameof(ResourceContract.GetElfTokenAddress));
            tokenAddress.DeserializeToString().ShouldBe(TokenContractAddress.GetFormatted());

            var address = Tester.GetAddress(FeeKeyPair);
            var feeAddressString = address.GetFormatted();
            var feeAddress = await Tester.CallContractMethodAsync(ResourceContractAddress, nameof(ResourceContract.GetFeeAddress));
            feeAddress.DeserializeToString().ShouldBe(feeAddressString);

            var controllerAddress = await Tester.CallContractMethodAsync(ResourceContractAddress, nameof(ResourceContract.GetResourceControllerAddress));
            controllerAddress.DeserializeToString().ShouldBe(feeAddressString);
        }

        [Fact]
        public async Task Query_Resource_ConverterInfo()
        {
            await Initialize_Resource();

            var cpuConverter = await Tester.CallContractMethodAsync(ResourceContractAddress, nameof(ResourceContract.GetConverter), "Cpu");
            var cpuString = cpuConverter.DeserializeToString();
            var cpuObj = JsonConvert.DeserializeObject<JObject>(cpuString);
            cpuObj["ResBalance"].ToObject<ulong>().ShouldBe(1000_000UL);
            cpuObj["ResWeight"].ToObject<ulong>().ShouldBe(500_000UL);
            cpuObj["ResourceType"].ToObject<string>().ShouldBe("Cpu");
        }

        [Fact]
        public async Task Query_Exchange_Balance()
        {
            await Initialize_Resource();

            var exchangeResult = await Tester.CallContractMethodAsync(ResourceContractAddress, nameof(ResourceContract.GetExchangeBalance), "Cpu");
            exchangeResult.DeserializeToUInt64().ShouldBe(1000_000UL);
        }

        [Fact]
        public async Task Query_Elf_Balance()
        {
            await Initialize_Resource();

            var elfResult = await Tester.CallContractMethodAsync(ResourceContractAddress, nameof(ResourceContract.GetElfBalance), "Cpu");
            elfResult.DeserializeToUInt64().ShouldBe(1000_000UL);
        }

        [Fact]
        public async Task IssueResource_With_Controller_Account()
        {
            await Initialize_Resource();

            Tester.SetCallOwner(FeeKeyPair);
            var issueResult = await Tester.ExecuteContractWithMiningAsync(ResourceContractAddress,
                nameof(ResourceContract.IssueResource),
                "Cpu", 100_000UL);
            
            
            issueResult.Status.ShouldBe(TransactionResultStatus.Mined);

            //check result
            var cpuConverter = await Tester.CallContractMethodAsync(ResourceContractAddress, nameof(ResourceContract.GetConverter), "Cpu");
            var cpuString = cpuConverter.DeserializeToString();
            var cpuObj = JsonConvert.DeserializeObject<JObject>(cpuString);
            cpuObj["ResBalance"].ToObject<ulong>().ShouldBe(1000_000UL + 100_000UL);
        }

        [Fact]
        public async Task IssueResource_WithNot_Controller_Account()
        {
            await Initialize_Resource();

            var otherKeyPair = CryptoHelpers.GenerateKeyPair();
            Tester.SetCallOwner(otherKeyPair);
            var issueResult = await Tester.ExecuteContractWithMiningAsync(ResourceContractAddress,
                nameof(ResourceContract.IssueResource),
                "CPU", 100_000UL);
            issueResult.Status.ShouldBe(TransactionResultStatus.Failed);
            issueResult.Error.Contains("Only resource controller is allowed to perform this action.").ShouldBe(true);
        }

        [Theory]
        [InlineData(10UL)]
        [InlineData(100UL)]
        [InlineData(1000UL)]
        [InlineData(10000UL)]
        public async Task Buy_Resource_WithEnough_Token(ulong paidElf)
        {
            await Initialize_Resource();
            var ownerAddress = Tester.GetAddress(Tester.CallOwnerKeyPair);

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
                await Tester.CallContractMethodAsync(TokenContractAddress, nameof(TokenContract.BalanceOf), ownerAddress);
            tokenBalance.DeserializeToUInt64().ShouldBe(1000_000UL - paidElf);

            var cpuBalance = await Tester.CallContractMethodAsync(ResourceContractAddress, nameof(ResourceContract.GetUserBalance),
                ownerAddress, "Cpu");
            cpuBalance.DeserializeToUInt64().ShouldBeGreaterThan(0UL);
        }

        [Fact]
        public async Task Buy_Resource_WithoutEnough_Token()
        {
            await Initialize_Resource();

            var noTokenKeyPair = CryptoHelpers.GenerateKeyPair();
            Tester.SetCallOwner(noTokenKeyPair);
            var buyResult = await Tester.ExecuteContractWithMiningAsync(ResourceContractAddress,
                nameof(ResourceContract.BuyResource),
                "Cpu", 10_000UL);
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
                "TestResource", 100UL);
            buyResult.Status.ShouldBe(TransactionResultStatus.Failed);
            buyResult.Error.Contains("Incorrect resource type.").ShouldBeTrue();
        }

        [Fact]
        public async Task Sell_WithEnough_Resource()
        {
            await Buy_Resource_WithEnough_Token(1000UL);

            var sellResult = await Tester.ExecuteContractWithMiningAsync(ResourceContractAddress,
                nameof(ResourceContract.SellResource),
                "Cpu", 100UL);
            sellResult.Status.ShouldBe(TransactionResultStatus.Mined);
        }

        [Fact]
        public async Task Sell_WithoutEnough_Resource()
        {
            await Buy_Resource_WithEnough_Token(100UL);

            var sellResult = await Tester.ExecuteContractWithMiningAsync(ResourceContractAddress,
                nameof(ResourceContract.SellResource),
                "Cpu", 1000UL);
            sellResult.Status.ShouldBe(TransactionResultStatus.Failed);
            sellResult.Error.Contains("Insufficient CPU balance.").ShouldBe(true);
        }

        [Fact]
        public async Task Sell_NotExist_Resource()
        {
            await Initialize_Resource();

            var sellResult = await Tester.ExecuteContractWithMiningAsync(ResourceContractAddress,
                nameof(ResourceContract.SellResource),
                "TestResource", 100UL);
            sellResult.Status.ShouldBe(TransactionResultStatus.Failed);
            sellResult.Error.Contains("Incorrect resource type.").ShouldBeTrue();
        }

        [Fact]
        public async Task Lock_Available_Resource()
        {
            await Buy_Resource_WithEnough_Token(1000UL);

            var ownerAddress = Tester.GetAddress(Tester.CallOwnerKeyPair);
            var resourceResult = await Tester.CallContractMethodAsync(ResourceContractAddress, nameof(ResourceContract.GetUserBalance),
                ownerAddress, "Cpu");
            var resourceBalance1 = resourceResult.DeserializeToUInt64();

            //Action
            var lockResult = await Tester.ExecuteContractWithMiningAsync(ResourceContractAddress, nameof(ResourceContract.LockResource),
                100UL, "Cpu");
            lockResult.Status.ShouldBe(TransactionResultStatus.Mined);

            //Verify
            resourceResult = await Tester.CallContractMethodAsync(ResourceContractAddress, nameof(ResourceContract.GetUserBalance),
                ownerAddress, "Cpu");
            var resourceBalance2 = resourceResult.DeserializeToUInt64();
            resourceBalance1.ShouldBe(resourceBalance2 + 100UL);

            var lockedResult = await Tester.CallContractMethodAsync(ResourceContractAddress, nameof(ResourceContract.GetUserLockedBalance),
                ownerAddress, "Cpu");
            var lockedBalance = lockedResult.DeserializeToUInt64();
            lockedBalance.ShouldBe(100UL);

            var controllerAddress = Tester.GetAddress(FeeKeyPair);
            var controllerResourceResult = await Tester.CallContractMethodAsync(ResourceContractAddress, nameof(ResourceContract.GetUserBalance),
                controllerAddress, "Cpu");
            var controllerBalance = controllerResourceResult.DeserializeToUInt64();
            controllerBalance.ShouldBe(100UL);
        }

        [Fact]
        public async Task Lock_OverOwn_Resource()
        {
            await Initialize_Resource();

            var lockResult = await Tester.ExecuteContractWithMiningAsync(ResourceContractAddress, nameof(ResourceContract.LockResource),
                1000UL, "Cpu");
            lockResult.Status.ShouldBe(TransactionResultStatus.Failed);
            lockResult.Error.Contains("System.OverflowException: Arithmetic operation resulted in an overflow.").ShouldBe(true);
        }

        [Fact]
        public async Task Unlock_Available_Resource()
        {
            await Buy_Resource_WithEnough_Token(1000UL);
            var ownerAddress = Tester.GetAddress(Tester.CallOwnerKeyPair);
            var resourceResult = await Tester.CallContractMethodAsync(ResourceContractAddress, nameof(ResourceContract.GetUserBalance),
                ownerAddress, "Cpu");
            var userBalance0 = resourceResult.DeserializeToUInt64();

            //Action
            var lockResult = await Tester.ExecuteContractWithMiningAsync(ResourceContractAddress, nameof(ResourceContract.LockResource),
                100UL, "Cpu");
            lockResult.Status.ShouldBe(TransactionResultStatus.Mined);

            var controllerAddress = Tester.GetAddress(FeeKeyPair);
            Tester.SetCallOwner(FeeKeyPair);
            var unlockResult = await Tester.ExecuteContractWithMiningAsync(ResourceContractAddress, nameof(ResourceContract.UnlockResource),
                ownerAddress, 50UL, "Cpu");
            unlockResult.Status.ShouldBe(TransactionResultStatus.Mined);

            //Verify
            resourceResult = await Tester.CallContractMethodAsync(ResourceContractAddress, nameof(ResourceContract.GetUserBalance),
                ownerAddress, "Cpu");
            var userBalance1 = resourceResult.DeserializeToUInt64();
            userBalance0.ShouldBe(userBalance1 + 50UL);

            var resource1Result = await Tester.CallContractMethodAsync(ResourceContractAddress, nameof(ResourceContract.GetUserBalance),
                controllerAddress, "Cpu");
            var controllerBalance = resource1Result.DeserializeToUInt64();
            controllerBalance.ShouldBe(50UL);

            var lockedResult = await Tester.CallContractMethodAsync(ResourceContractAddress, nameof(ResourceContract.GetUserLockedBalance),
                ownerAddress, "Cpu");
            var lockedBalance = lockedResult.DeserializeToUInt64();
            lockedBalance.ShouldBe(50UL);
        }

        [Fact]
        public async Task Unlock_WithNot_Controller()
        {
            await Buy_Resource_WithEnough_Token(1000UL);
            var ownerAddress = Tester.GetAddress(Tester.CallOwnerKeyPair);

            //Action
            var lockResult = await Tester.ExecuteContractWithMiningAsync(ResourceContractAddress, nameof(ResourceContract.LockResource),
                100UL, "Cpu");
            lockResult.Status.ShouldBe(TransactionResultStatus.Mined);

            var unlockResult = await Tester.ExecuteContractWithMiningAsync(ResourceContractAddress, nameof(ResourceContract.UnlockResource),
                ownerAddress, 50UL, "Cpu");
            unlockResult.Status.ShouldBe(TransactionResultStatus.Failed);
            unlockResult.Error.Contains("Only the resource controller can perform this action.").ShouldBeTrue();
        }

        [Fact]
        public async Task Unlock_OverLocked_Resource()
        {
            await Buy_Resource_WithEnough_Token(1000UL);
            var ownerAddress = Tester.GetAddress(Tester.CallOwnerKeyPair);

            //Action
            var lockResult = await Tester.ExecuteContractWithMiningAsync(ResourceContractAddress, nameof(ResourceContract.LockResource),
                100UL, "Cpu");
            lockResult.Status.ShouldBe(TransactionResultStatus.Mined);

            Tester.SetCallOwner(FeeKeyPair);
            var unlockResult = await Tester.ExecuteContractWithMiningAsync(ResourceContractAddress, nameof(ResourceContract.UnlockResource),
                ownerAddress, 200UL, "Cpu");
            unlockResult.Status.ShouldBe(TransactionResultStatus.Failed);
            unlockResult.Error.Contains("Arithmetic operation resulted in an overflow.").ShouldBeTrue();
        }

        private async Task ApproveBalance(ulong amount)
        {
            var callOwner = Tester.GetAddress(Tester.CallOwnerKeyPair);

            var resourceResult = await Tester.ExecuteContractWithMiningAsync(TokenContractAddress, nameof(TokenContract.Approve),
                ResourceContractAddress, amount);
            resourceResult.Status.ShouldBe(TransactionResultStatus.Mined);
            var allowanceResult1 = await Tester.CallContractMethodAsync(TokenContractAddress, nameof(TokenContract.Allowance),
                callOwner, ResourceContractAddress);
            Console.WriteLine($"Allowance Query: {ResourceContractAddress} = {allowanceResult1.DeserializeToUInt64()}");
        }

        #endregion
    }
}