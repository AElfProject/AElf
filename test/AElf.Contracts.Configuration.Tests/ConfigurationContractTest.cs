using System.Threading.Tasks;
using Acs1;
using Acs3;
using AElf.Kernel;
using AElf.Types;
using AElf.Contracts.Configuration;
using AElf.Contracts.Parliament;
using AElf.Kernel.Configuration;
using AElf.Kernel.SmartContractExecution.Application;
using Google.Protobuf;
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

            var limitFromResult = new Int32Value();
            limitFromResult.MergeFrom(ConfigurationSet.Parser.ParseFrom(transactionResult.Logs[1].NonIndexed).Value);
            limitFromResult.Value.ShouldBe(100);
        }

        [Fact]
        public async Task Set_Block_Transaction_Limit_NotAuthorized()
        {
            var transactionResult =
                await ExecuteContractWithMiningAsync(ConfigurationContractAddress,
                    nameof(ConfigurationContainer.ConfigurationStub.SetConfiguration),
                    new SetConfigurationInput
                    {
                        Key = BlockTransactionLimitConfigurationNameProvider.Name,
                        Value = new Int32Value {Value = 100}.ToByteString()
                    });
            var status = transactionResult.Status;
            Assert.True(status == TransactionResultStatus.Failed);
            Assert.Contains("No permission.", transactionResult.Error);
        }

        [Fact]
        public async Task Get_Block_Transaction_Limit()
        {
            var proposalId = await SetBlockTransactionLimitProposalAsync(100);
            await ApproveWithMinersAsync(proposalId);
            await ReleaseProposalAsync(proposalId);

            var transactionResult =
                await ExecuteContractWithMiningAsync(ConfigurationContractAddress,
                    nameof(ConfigurationContainer.ConfigurationStub.GetConfiguration),
                    new StringValue {Value = BlockTransactionLimitConfigurationNameProvider.Name});
            Assert.True(transactionResult.Status == TransactionResultStatus.Mined);
            var limitFromResult = new Int32Value();
            limitFromResult.MergeFrom(BytesValue.Parser.ParseFrom(transactionResult.ReturnValue).Value);
            limitFromResult.Value.ShouldBe(100);
        }

        [Fact]
        public async Task Change_Owner_Address_Authorized()
        {
            var sender = SampleAddress.AddressList[0];
            _testOutputHelper.WriteLine(sender.ToBase58());
            var newOrganization = Address.Parser.ParseFrom((await Tester.ExecuteContractWithMiningAsync(ParliamentAddress,
                nameof(ParliamentContractContainer.ParliamentContractStub.CreateOrganization),
                new CreateOrganizationInput
                {
                    ProposalReleaseThreshold = new ProposalReleaseThreshold
                    {
                        MinimalApprovalThreshold = 1,
                        MinimalVoteThreshold = 1
                    },
                    ParliamentMemberProposingAllowed = true
                })).ReturnValue);
            var proposalId = await SetTransactionOwnerAddressProposalAsync(new AuthorityInfo
            {
                ContractAddress = ParliamentAddress,
                OwnerAddress = newOrganization
            });
            await ApproveWithMinersAsync(proposalId);
            var transactionResult = await ReleaseProposalAsync(proposalId);
            Assert.True(transactionResult.Status == TransactionResultStatus.Mined);

            var transactionResult2 =
                await ExecuteContractWithMiningAsync(ConfigurationContractAddress,
                    nameof(ConfigurationContainer.ConfigurationStub.GetConfigurationController),
                    new Empty());
            var authorityInfo = AuthorityInfo.Parser.ParseFrom(transactionResult2.ReturnValue);
            Assert.True(newOrganization == authorityInfo.OwnerAddress);
        }

        [Fact]
        public async Task Change_Owner_Address_NotAuthorized()
        {
            var transactionResult =
                await ExecuteContractWithMiningAsync(ConfigurationContractAddress,
                    nameof(ConfigurationContainer.ConfigurationStub.ChangeConfigurationController),
                    new AuthorityInfo
                    {
                        ContractAddress = ParliamentAddress,
                        OwnerAddress = SampleAddress.AddressList[0]
                    });
            var status = transactionResult.Status;
            Assert.True(status == TransactionResultStatus.Failed);
            Assert.Contains("No permission.", transactionResult.Error);
        }

        [Fact]
        public async Task SetRequiredAcsInContracts_NoPermission()
        {
            var transactionResult = await ExecuteContractWithMiningAsync(ConfigurationContractAddress,
                nameof(ConfigurationContainer.ConfigurationStub.SetConfiguration),
                new SetConfigurationInput
                {
                    Key = RequiredAcsInContractsConfigurationNameProvider.Name,
                    Value = new RequiredAcsInContracts().ToByteString()
                });

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
            var proposalId = await CreateProposalAsync(organizationAddress, new SetConfigurationInput
                {
                    Key = RequiredAcsInContractsConfigurationNameProvider.Name,
                    Value = contractFeeChargingPolicy.ToByteString()
                },
                nameof(ConfigurationContainer.ConfigurationStub.SetConfiguration));
            proposalId.ShouldNotBeNull();
            await ApproveWithMinersAsync(proposalId);
            var releaseTxResult = await ReleaseProposalAsync(proposalId);
            releaseTxResult.Status.ShouldBe(TransactionResultStatus.Mined);
            var actual = await Tester.CallContractMethodAsync(ConfigurationContractAddress,
                nameof(ConfigurationContainer.ConfigurationStub.GetConfiguration),
                new StringValue
                {
                    Value = RequiredAcsInContractsConfigurationNameProvider.Name
                });
            RequiredAcsInContracts.Parser.ParseFrom(BytesValue.Parser.ParseFrom(actual).Value)
                .ShouldBe(contractFeeChargingPolicy);
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
        [Fact]
        public async Task SetMethodFee_Failed_Test()
        {
            var result = await Tester.ExecuteContractWithMiningAsync(ConfigurationContractAddress,
                nameof(ConfigurationContainer.ConfigurationStub.SetMethodFee),
                new MethodFees
                {
                    MethodName = "name",
                    Fees = {new MethodFee
                    {
                        Symbol ="ELF" ,BasicFee = 2
                    }}
                });
            result.Status.ShouldBe(TransactionResultStatus.Failed);
            result.Error.Contains("Unauthorized").ShouldBeTrue();
        }
        [Fact]
        public async Task SetMethodFee_Success_Test()
        {
            //SetMethodFee Test
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
                nameof(ConfigurationContainer.ConfigurationStub.SetMethodFee);
            var proposalId = await CreateProposalAsync(Tester, methodFeeController.ContractAddress,
                methodFeeController.OwnerAddress, proposalCreationMethodName,
                new MethodFees
                {
                    MethodName = "name",
                    Fees = {new MethodFee
                    {
                        Symbol ="ELF" ,BasicFee = 2
                    }}
                });
            await ApproveWithMinersAsync(proposalId);
            var txResult = await ReleaseProposalAsync(proposalId);
            txResult.Status.ShouldBe(TransactionResultStatus.Mined);
            //GetMethodFee Test
            var transactionResult = await ExecuteContractWithMiningAsync(ConfigurationContractAddress,
                nameof(ConfigurationContainer.ConfigurationStub.GetMethodFee),
               new StringValue
               {
                   Value = "name"
               }
               );
            transactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
            transactionResult.ReturnValue.ShouldNotBeNull();
        }
    }
}
