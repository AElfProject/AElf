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
using Google.Protobuf.WellKnownTypes;
using Google.Protobuf;
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
                nameof(ISmartContractZero.DeploySmartContract),
                new ContractDeploymentInput()
                {
                    Category = SmartContractTestConstants.TestRunnerCategory,
                    Code = ByteString.CopyFrom(File.ReadAllBytes(typeof(TokenContract).Assembly.Location))
                });
            var resourceTx = await Tester.GenerateTransactionAsync(BasicZeroContractAddress,
                nameof(ISmartContractZero.DeploySmartContract),
                new ContractDeploymentInput()
                {
                    Category = SmartContractTestConstants.TestRunnerCategory,
                    Code = ByteString.CopyFrom(File.ReadAllBytes(typeof(ResourceContract).Assembly.Location))
                });

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
                new FeeReceiver.InitializeInput()
                {
                    ElfTokenAddress = TokenContractAddress,
                    FoundationAddress = foundationAddress
                });
            feeReceiverResult.Status.ShouldBe(TransactionResultStatus.Mined);

            //init resource contract
            var feeAddress = Tester.GetAddress(FeeKeyPair);
            var resourceResult = await Tester.ExecuteContractWithMiningAsync(ResourceContractAddress,
                nameof(ResourceContract.Initialize),
                new InitializeInput()
                {
                    ElfTokenAddress = TokenContractAddress,
                    FeeAddress = feeAddress,
                    ResourceControllerAddress = feeAddress
                });
            resourceResult.Status.ShouldBe(TransactionResultStatus.Mined);
        }

        #region Resource Contract cases

        [Fact]
        public async Task Query_Resource_AddressInfo()
        {
            await Initialize_Resource();

            //verify result
            var tokenAddress = await Tester.CallContractMethodAsync(ResourceContractAddress,
                nameof(ResourceContract.GetElfTokenAddress), new Empty());
            Address.Parser.ParseFrom(tokenAddress).ShouldBe(TokenContractAddress);

            var address = Tester.GetAddress(FeeKeyPair);
            var feeAddress =
                await Tester.CallContractMethodAsync(ResourceContractAddress,
                    nameof(ResourceContract.GetFeeAddress), new Empty());
            Address.Parser.ParseFrom(feeAddress).ShouldBe(address);

            var controllerAddress = await Tester.CallContractMethodAsync(ResourceContractAddress,
                nameof(ResourceContract.GetResourceControllerAddress), new Empty());
            Address.Parser.ParseFrom(controllerAddress).ShouldBe(address);
        }

        [Fact]
        public async Task Query_Resource_ConverterInfo()
        {
            await Initialize_Resource();

            var cpuConverter = Converter.Parser.ParseFrom(
                await Tester.CallContractMethodAsync(ResourceContractAddress,
                    nameof(ResourceContract.GetConverter),
                    new ResourceId() {Type = ResourceType.Cpu}));

            cpuConverter.ResBalance.ShouldBe(1000_000L);
            cpuConverter.ResWeight.ShouldBe(500_000L);
            cpuConverter.Type.ShouldBe(ResourceType.Cpu);
        }

        [Fact]
        public async Task Query_Exchange_Balance()
        {
            await Initialize_Resource();

            var exchangeResult = SInt64Value.Parser.ParseFrom(
                await Tester.CallContractMethodAsync(ResourceContractAddress,
                    nameof(ResourceContract.GetExchangeBalance),
                    new ResourceId() {Type = ResourceType.Cpu})).Value;
            exchangeResult.ShouldBe(1000_000L);
        }

        [Fact]
        public async Task Query_Elf_Balance()
        {
            await Initialize_Resource();

            var elfResult = SInt64Value.Parser.ParseFrom(
                await Tester.CallContractMethodAsync(ResourceContractAddress,
                nameof(ResourceContract.GetElfBalance), 
                new ResourceId() {Type = ResourceType.Cpu})).Value;
            elfResult.ShouldBe(1000_000L);
        }

        [Fact]
        public async Task IssueResource_With_Controller_Account()
        {
            await Initialize_Resource();

            var receiver = Tester.CreateNewContractTester(FeeKeyPair);
            var issueResult = await receiver.ExecuteContractWithMiningAsync(ResourceContractAddress,
                nameof(ResourceContract.IssueResource),
                new ResourceAmount()
                {
                    Type = ResourceType.Cpu,
                    Amount = 100_000L
                });

            issueResult.Status.ShouldBe(TransactionResultStatus.Mined);

            //check result
            var cpuConverter = Converter.Parser.ParseFrom(await receiver.CallContractMethodAsync(
                ResourceContractAddress,
                nameof(ResourceContract.GetConverter), 
                new ResourceId()
                {
                    Type = ResourceType.Cpu
                }));

            cpuConverter.ResBalance.ShouldBe(1000_000L + 100_000L);
        }

        [Fact]
        public async Task IssueResource_WithNot_Controller_Account()
        {
            await Initialize_Resource();

            var otherKeyPair = Tester.KeyPair;
            var issueResult = await Tester.ExecuteContractWithMiningAsync(ResourceContractAddress,
                nameof(ResourceContract.IssueResource),
                new ResourceAmount()
                {
                    Type = ResourceType.Cpu,
                    Amount = 100_000L
                });
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
                new ResourceAmount()
                {
                    Type = ResourceType.Cpu,
                    Amount = paidElf
                });
            var returnMessage = buyResult.ReturnValue.ToStringUtf8();
            returnMessage.ShouldBe(string.Empty);
            buyResult.Status.ShouldBe(TransactionResultStatus.Mined);

            //Check result
            var tokenBalance = GetBalanceOutput.Parser.ParseFrom(
                await Tester.CallContractMethodAsync(TokenContractAddress, 
                    nameof(TokenContract.GetBalance),
                    new GetBalanceInput
                    {
                        Owner = ownerAddress,
                        Symbol = "ELF"
                    }));
            tokenBalance.Balance.ShouldBe(1000_000L - paidElf);

            var cpuBalance =SInt64Value.Parser.ParseFrom(await Tester.CallContractMethodAsync(ResourceContractAddress,
                nameof(ResourceContract.GetUserBalance),
                new UserResourceId()
                {
                    Address = ownerAddress,
                    Type = ResourceType.Cpu
                })).Value;
            cpuBalance.ShouldBeGreaterThan(0L);
        }

        [Fact]
        public async Task Buy_Resource_WithoutEnough_Token()
        {
            await Initialize_Resource();

            var noTokenKeyPair = Tester.KeyPair;
            var buyResult = await Tester.ExecuteContractWithMiningAsync(ResourceContractAddress,
                nameof(ResourceContract.BuyResource),
                new ResourceAmount()
                {
                    Type = ResourceType.Cpu,
                    Amount = 10_000L
                });
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
                new ResourceAmount()
                {
                    Type = (ResourceType)99,
                    Amount = 100L
                });
            buyResult.Status.ShouldBe(TransactionResultStatus.Failed);
            buyResult.Error.ShouldContain("Incorrect resource type.");
        }

        [Fact]
        public async Task Sell_WithEnough_Resource()
        {
            await Buy_Resource_WithEnough_Token(1000L);

            var sellResult = await Tester.ExecuteContractWithMiningAsync(ResourceContractAddress,
                nameof(ResourceContract.SellResource),
                new ResourceAmount()
                {
                    Type = ResourceType.Cpu,
                    Amount = 100L
                });
            sellResult.Status.ShouldBe(TransactionResultStatus.Mined);
        }

        [Fact]
        public async Task Sell_WithoutEnough_Resource()
        {
            await Buy_Resource_WithEnough_Token(100L);

            var sellResult = await Tester.ExecuteContractWithMiningAsync(ResourceContractAddress,
                nameof(ResourceContract.SellResource),
                new ResourceAmount()
                {
                    Type = ResourceType.Cpu,
                    Amount = 1000L
                });
            sellResult.Status.ShouldBe(TransactionResultStatus.Failed);
            sellResult.Error.Contains("Insufficient CPU balance.").ShouldBe(true);
        }

        [Fact]
        public async Task Sell_NotExist_Resource()
        {
            await Initialize_Resource();

            var sellResult = await Tester.ExecuteContractWithMiningAsync(ResourceContractAddress,
                nameof(ResourceContract.SellResource),
                new ResourceAmount()
                {
                    Type = (ResourceType) 99,
                    Amount = 100L
                });
            sellResult.Status.ShouldBe(TransactionResultStatus.Failed);
            sellResult.Error.Contains("Incorrect resource type.").ShouldBeTrue();
        }

        [Fact]
        public async Task Lock_Available_Resource()
        {
            await Buy_Resource_WithEnough_Token(1000L);

            var ownerAddress = Tester.GetAddress(Tester.KeyPair);
            var resourceBalance1 =SInt64Value.Parser.ParseFrom(await Tester.CallContractMethodAsync(
                ResourceContractAddress,
                nameof(ResourceContract.GetUserBalance),
                new UserResourceId()
                {
                    Address = ownerAddress,
                    Type = ResourceType.Cpu
                })).Value;

            //Action
            var lockResult = await Tester.ExecuteContractWithMiningAsync(ResourceContractAddress,
                nameof(ResourceContract.LockResource),
                new ResourceAmount()
                {
                    Type = ResourceType.Cpu,
                    Amount = 100L
                });
            lockResult.Status.ShouldBe(TransactionResultStatus.Mined);

            //Verify
            var resourceBalance2 =SInt64Value.Parser.ParseFrom(
                await Tester.CallContractMethodAsync(ResourceContractAddress,
                nameof(ResourceContract.GetUserBalance),
                new UserResourceId()
                {
                    Address = ownerAddress,
                    Type = ResourceType.Cpu
                })).Value;
            resourceBalance1.ShouldBe(resourceBalance2 + 100L);

            var lockedBalance =SInt64Value.Parser.ParseFrom(await Tester.CallContractMethodAsync(ResourceContractAddress,
                nameof(ResourceContract.GetUserLockedBalance),
                new UserResourceId()
                {
                    Address = ownerAddress,
                    Type = ResourceType.Cpu
                })).Value;
            lockedBalance.ShouldBe(100L);

            var controllerAddress = Tester.GetAddress(FeeKeyPair);
            var controllerBalance =SInt64Value.Parser.ParseFrom(await Tester.CallContractMethodAsync(ResourceContractAddress,
                nameof(ResourceContract.GetUserBalance),
                new UserResourceId()
                {
                    Address = controllerAddress,
                    Type = ResourceType.Cpu
                })).Value;
            controllerBalance.ShouldBe(100L);
        }

        [Fact(Skip = "long type won't throw exception, maybe need another way to test.")]
        public async Task Lock_OverOwn_Resource()
        {
            await Initialize_Resource();

            var lockResult = await Tester.ExecuteContractWithMiningAsync(ResourceContractAddress,
                nameof(ResourceContract.LockResource),
                new ResourceAmount()
                {
                    Type = ResourceType.Cpu,
                    Amount = 1000L
                });
            lockResult.Status.ShouldBe(TransactionResultStatus.Failed);
            lockResult.Error.Contains("System.OverflowException: Arithmetic operation resulted in an overflow.")
                .ShouldBe(true);
        }

        [Fact]
        public async Task Unlock_Available_Resource()
        {
            await Buy_Resource_WithEnough_Token(1000L);
            var ownerAddress = Tester.GetAddress(Tester.KeyPair);
            var userBalance0 =SInt64Value.Parser.ParseFrom(await Tester.CallContractMethodAsync(ResourceContractAddress,
                nameof(ResourceContract.GetUserBalance),
                new UserResourceId()
                {
                    Address = ownerAddress,
                    Type = ResourceType.Cpu
                })).Value;

            //Action
            var lockResult = await Tester.ExecuteContractWithMiningAsync(ResourceContractAddress,
                nameof(ResourceContract.LockResource),
                new ResourceAmount()
                {
                    Type = ResourceType.Cpu,
                    Amount = 100L
                });
            lockResult.Status.ShouldBe(TransactionResultStatus.Mined);

            var controllerAddress = Tester.GetAddress(FeeKeyPair);
            var receiver = Tester.CreateNewContractTester(FeeKeyPair);
            var unlockResult = await receiver.ExecuteContractWithMiningAsync(ResourceContractAddress,
                nameof(ResourceContract.UnlockResource),
                new UserResourceAmount()
                {
                    User = ownerAddress,
                    Type = ResourceType.Cpu,
                    Amount = 50L
                });
            unlockResult.Status.ShouldBe(TransactionResultStatus.Mined);

            //Verify
            var userBalance1 =SInt64Value.Parser.ParseFrom(await receiver.CallContractMethodAsync(ResourceContractAddress,
                nameof(ResourceContract.GetUserBalance),
                new UserResourceId()
                {
                    Type = ResourceType.Cpu,
                    Address = ownerAddress
                })).Value;
            userBalance0.ShouldBe(userBalance1 + 50L);

            var controllerBalance =SInt64Value.Parser.ParseFrom(await receiver.CallContractMethodAsync(ResourceContractAddress,
                nameof(ResourceContract.GetUserBalance),
                new UserResourceId()
                {
                    Address = controllerAddress,
                    Type = ResourceType.Cpu
                })).Value;
            controllerBalance.ShouldBe(50L);

            var lockedBalance =SInt64Value.Parser.ParseFrom(await receiver.CallContractMethodAsync(ResourceContractAddress,
                nameof(ResourceContract.GetUserLockedBalance),
                new UserResourceId()
                {
                    Address = ownerAddress,
                    Type = ResourceType.Cpu
                })).Value;
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
                new ResourceAmount()
                {
                    Type = ResourceType.Cpu,
                    Amount = 100L
                });
            lockResult.Status.ShouldBe(TransactionResultStatus.Mined);

            var unlockResult = await Tester.ExecuteContractWithMiningAsync(ResourceContractAddress,
                nameof(ResourceContract.UnlockResource),
                new UserResourceAmount()
                {
                    User = ownerAddress,
                    Type = ResourceType.Cpu,
                    Amount = 50L
                });
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
                new ResourceAmount()
                {
                    Type = ResourceType.Cpu,
                    Amount = 100L
                });
            lockResult.Status.ShouldBe(TransactionResultStatus.Mined);

            var receiver = Tester.CreateNewContractTester(FeeKeyPair);
            var unlockResult = await receiver.ExecuteContractWithMiningAsync(ResourceContractAddress,
                nameof(ResourceContract.UnlockResource),
                new UserResourceAmount()
                {
                    User = ownerAddress,
                    Type = ResourceType.Cpu,
                    Amount = 200L
                });
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
            var allowanceResult1 = GetAllowanceOutput.Parser.ParseFrom(
                await Tester.CallContractMethodAsync(TokenContractAddress,
                    nameof(TokenContract.GetAllowance), new GetAllowanceInput
                    {
                        Owner = callOwner,
                        Spender = ResourceContractAddress,
                        Symbol = "ELF"
                    }));
            Console.WriteLine(
                $"Allowance Query: {ResourceContractAddress} = {allowanceResult1.Allowance}");
        }

        #endregion
    }
}