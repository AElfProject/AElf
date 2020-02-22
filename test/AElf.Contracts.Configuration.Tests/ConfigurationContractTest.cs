using System.Linq;
using System.Threading.Tasks;
using Acs1;
using Acs3;
using AElf.Kernel;
using AElf.Types;
using AElf.Contracts.Configuration;
using AElf.Contracts.Parliament;
using AElf.Contracts.TestBase;
using Google.Protobuf;
using Google.Protobuf.Collections;
using Google.Protobuf.WellKnownTypes;
using Shouldly;
using Xunit;
using Xunit.Abstractions;

namespace AElf.Contracts.ConfigurationContract.Tests
{
    public class ConfigurationContractTest : ConfigurationContractTestBase
    {
        private readonly ITestOutputHelper _testOutputHelper;

        public ConfigurationContractTest(ITestOutputHelper testOutputHelper)
        {
            _testOutputHelper = testOutputHelper;
        }

        [Fact]
        public async Task Set_Block_Transaction_Limit_Authorized()
        {
            var proposalId = await SetBlockTransactionLimitProposalAsync(100);
            await ApproveWithMinersAsync(proposalId);
            var transactionResult = await ReleaseProposalAsync(proposalId);

            Assert.True(transactionResult.Status == TransactionResultStatus.Mined);

            var oldLimit = BlockTransactionLimitChanged.Parser.ParseFrom(transactionResult.Logs[1].NonIndexed).Old;
            var newLimit = BlockTransactionLimitChanged.Parser.ParseFrom(transactionResult.Logs[1].NonIndexed).New;

            Assert.True(oldLimit == 0);
            Assert.True(newLimit == 100);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        [InlineData(-2)]
        public async Task Set_Block_Transaction_Limit_InvalidInput(int amount)
        {
            var proposalId = await SetBlockTransactionLimitProposalAsync(amount);
            await ApproveWithMinersAsync(proposalId);
            var transactionResult = await ReleaseProposalAsync(proposalId);

            Assert.True(transactionResult.Status == TransactionResultStatus.Failed);
            Assert.Contains("Invalid input.", transactionResult.Error);
        }

        [Fact]
        public async Task Set_Block_Transaction_Limit_NotAuthorized()
        {
            var transactionResult =
                await ExecuteContractWithMiningAsync(ConfigurationContractAddress,
                    nameof(ConfigurationContainer.ConfigurationStub.SetBlockTransactionLimit),
                    new Int32Value()
                    {
                        Value = 100
                    });
            var status = transactionResult.Status;
            Assert.True(status == TransactionResultStatus.Failed);
            Assert.Contains("Not authorized to do this.", transactionResult.Error);
        }

        [Fact]
        public async Task Get_Block_Transaction_Limit()
        {
            var proposalId = await SetBlockTransactionLimitProposalAsync(100);
            await ApproveWithMinersAsync(proposalId);
            await ReleaseProposalAsync(proposalId);

            var transactionResult =
                await ExecuteContractWithMiningAsync(ConfigurationContractAddress,
                    nameof(ConfigurationContainer.ConfigurationStub.GetBlockTransactionLimit),
                    new Empty());
            Assert.True(transactionResult.Status == TransactionResultStatus.Mined);
            var oldLimit = BlockTransactionLimitChanged.Parser.ParseFrom(transactionResult.ReturnValue).Old;
            var newLimit = BlockTransactionLimitChanged.Parser.ParseFrom(transactionResult.ReturnValue).New;
            var limit = Int32Value.Parser.ParseFrom(transactionResult.ReturnValue).Value;

            Assert.True(oldLimit == 100);
            Assert.True(newLimit == 0);
            Assert.True(limit == 100);
        }

        [Fact]
        public async Task Change_Owner_Address_Authorized()
        {
            var address1 = SampleAddress.AddressList[0];
            _testOutputHelper.WriteLine(address1.GetFormatted());
            var proposalId = await SetTransactionOwnerAddressProposalAsync(address1);
            await ApproveWithMinersAsync(proposalId);
            var transactionResult = await ReleaseProposalAsync(proposalId);
            Assert.True(transactionResult.Status == TransactionResultStatus.Mined);

            var transactionResult2 =
                await ExecuteContractWithMiningAsync(ConfigurationContractAddress,
                    nameof(ConfigurationContainer.ConfigurationStub.GetConfigurationController),
                    new Empty());
            var address2 = Address.Parser.ParseFrom(transactionResult2.ReturnValue);
            _testOutputHelper.WriteLine(address2.GetFormatted());
            Assert.True(address1 == address2);
        }

        [Fact]
        public async Task Change_Owner_Address_NotAuthorized()
        {
            var transactionResult =
                await ExecuteContractWithMiningAsync(ConfigurationContractAddress,
                    nameof(ConfigurationContainer.ConfigurationStub.ChangeConfigurationController),
                    SampleAddress.AddressList[0]);
            var status = transactionResult.Status;
            Assert.True(status == TransactionResultStatus.Failed);
            Assert.Contains("Not authorized to do this.", transactionResult.Error);
        }

        [Fact]
        public async Task InitialTotalResourceTokensTest()
        {
            // Check total resource token amount.
            {
                var transactionResult =
                    await ExecuteContractWithMiningAsync(ConfigurationContractAddress,
                        nameof(ConfigurationContainer.ConfigurationStub.GetTotalResourceTokens),
                        new Empty());
                var resourceTokenAmount = new ResourceTokenAmount();
                resourceTokenAmount.MergeFrom(transactionResult.ReturnValue);
                resourceTokenAmount.Value.Keys.ShouldContain("CPU");
                resourceTokenAmount.Value["CPU"].ShouldBe(SmartContractTestConstants.ResourceSupply);
                resourceTokenAmount.Value.Keys.ShouldContain("RAM");
                resourceTokenAmount.Value["RAM"].ShouldBe(SmartContractTestConstants.ResourceSupply);
                resourceTokenAmount.Value.Keys.ShouldContain("DISK");
                resourceTokenAmount.Value["DISK"].ShouldBe(SmartContractTestConstants.ResourceSupply);
                resourceTokenAmount.Value.Keys.ShouldContain("NET");
                resourceTokenAmount.Value["NET"].ShouldBe(SmartContractTestConstants.ResourceSupply);
            }

            // Check remain resource token amount.
            {
                var transactionResult =
                    await ExecuteContractWithMiningAsync(ConfigurationContractAddress,
                        nameof(ConfigurationContainer.ConfigurationStub.GetRemainResourceTokens),
                        new Empty());
                var resourceTokenAmount = new ResourceTokenAmount();
                resourceTokenAmount.MergeFrom(transactionResult.ReturnValue);
                resourceTokenAmount.Value.Keys.ShouldContain("CPU");
                resourceTokenAmount.Value["CPU"].ShouldBe(SmartContractTestConstants.ResourceSupply);
                resourceTokenAmount.Value.Keys.ShouldContain("RAM");
                resourceTokenAmount.Value["RAM"].ShouldBe(SmartContractTestConstants.ResourceSupply);
                resourceTokenAmount.Value.Keys.ShouldContain("DISK");
                resourceTokenAmount.Value["DISK"].ShouldBe(SmartContractTestConstants.ResourceSupply);
                resourceTokenAmount.Value.Keys.ShouldContain("NET");
                resourceTokenAmount.Value["NET"].ShouldBe(SmartContractTestConstants.ResourceSupply);
            }
        }

        [Fact]
        public async Task SetRequiredAcsInContracts_NoPermission()
        {
            var transactionResult = await ExecuteContractWithMiningAsync(ConfigurationContractAddress,
                nameof(ConfigurationContainer.ConfigurationStub.SetRequiredAcsInContracts),
                new RequiredAcsInContracts());
            
            var test = new RequiredAcsInContracts();
            transactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
            transactionResult.Error.ShouldContain("No permission.");
        }

        [Fact]
        public async Task SetRequiredAcsInContracts_Test()
        {
            var contractFeeChargingPolicy = new RequiredAcsInContracts
            {
                AcsList = {"acsx", "acsy"}
            };
            var organizationAddress = await GetParliamentDefaultOrganizationAddressAsync();
            var proposalId = await CreateProposalAsync(organizationAddress, contractFeeChargingPolicy, 
                nameof(ConfigurationContainer.ConfigurationStub.SetRequiredAcsInContracts));
            proposalId.ShouldNotBeNull();
            await ApproveWithMinersAsync(proposalId);
            var releaseTxResult = await ReleaseProposalAsync(proposalId);
            releaseTxResult.Status.ShouldBe(TransactionResultStatus.Mined);
            var actual = await Tester.CallContractMethodAsync(ConfigurationContractAddress,
                nameof(ConfigurationContainer.ConfigurationStub.GetRequiredAcsInContracts), new Empty());
            RequiredAcsInContracts.Parser.ParseFrom(actual).ShouldBe(contractFeeChargingPolicy);
        }

        [Fact]
        public async Task ChangeMethodFeeController_Test()
        {
            var createOrganizationResult = await Tester.ExecuteContractWithMiningAsync(ParliamentAddress,
                nameof(ParliamentContractContainer.ParliamentContractStub.CreateOrganization),
                new CreateOrganizationInput
                {
                    ProposalReleaseThreshold = new ProposalReleaseThreshold
                    {
                        MinimalApprovalThreshold = 1000,
                        MinimalVoteThreshold = 1000
                    }
                });

            var organizationAddress = Address.Parser.ParseFrom(createOrganizationResult.ReturnValue);

            var methodFeeController = await GetMethodFeeController(ConfigurationContractAddress);
            const string proposalCreationMethodName =
                nameof(ConfigurationContainer.ConfigurationStub.ChangeMethodFeeController);
            var proposalId = await CreateProposalAsync(Tester, methodFeeController.ContractAddress,
                methodFeeController.OwnerAddress, proposalCreationMethodName,
                new AuthorityInfo
                {
                    OwnerAddress = organizationAddress,
                    ContractAddress = methodFeeController.ContractAddress
                });
            await ApproveWithMinersAsync(proposalId);
            var txResult2 = await ReleaseProposalAsync(proposalId);
            txResult2.Status.ShouldBe(TransactionResultStatus.Mined);

            var newMethodFeeController = await GetMethodFeeController(ConfigurationContractAddress);
            Assert.True(newMethodFeeController.OwnerAddress == organizationAddress);
        }

        [Fact]
        public async Task ChangeMethodFeeController_WithoutAuth_Test()
        {
            var result = await Tester.ExecuteContractWithMiningAsync(ConfigurationContractAddress,
                nameof(ConfigurationContainer.ConfigurationStub.ChangeMethodFeeController),
                new AuthorityInfo()
                {
                    OwnerAddress = Tester.GetCallOwnerAddress(),
                    ContractAddress = ParliamentAddress
                });

            result.Status.ShouldBe(TransactionResultStatus.Failed);
            result.Error.Contains("Unauthorized behavior.").ShouldBeTrue();

            // Invalid organization address
            var methodFeeController = await GetMethodFeeController(ConfigurationContractAddress);
            const string proposalCreationMethodName =
                nameof(ConfigurationContainer.ConfigurationStub.ChangeMethodFeeController);
            var proposalId = await CreateProposalAsync(Tester, methodFeeController.ContractAddress,
                methodFeeController.OwnerAddress, proposalCreationMethodName,
                new AuthorityInfo
                {
                    OwnerAddress = SampleAddress.AddressList[4],
                    ContractAddress = methodFeeController.ContractAddress
                });
            await ApproveWithMinersAsync(proposalId);
            var txResult2 = await ReleaseProposalAsync(proposalId);
            txResult2.Status.ShouldBe(TransactionResultStatus.Failed);
            txResult2.Error.Contains("Invalid authority input.").ShouldBeTrue();
        }
    }
}
