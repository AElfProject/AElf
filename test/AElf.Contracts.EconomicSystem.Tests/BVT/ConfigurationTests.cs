using System.Linq;
using System.Threading.Tasks;
using Acs1;
using Acs3;
using AElf.Contracts.Configuration;
using AElf.Contracts.Parliament;
using AElf.Kernel.Configuration;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Shouldly;
using Xunit;

namespace AElf.Contracts.EconomicSystem.Tests.BVT
{
    partial class EconomicSystemTest
    {
        [Fact]
        public async Task SetConfiguration_TransactionFee_Test()
        {
            await ExecuteProposalTransaction(Tester, ConfigurationAddress,
                nameof(ConfigurationContractStub.SetMethodFee),
                new MethodFees
                {
                    MethodName = nameof(ConfigurationContractStub.SetConfiguration),
                    Fees =
                    {
                        new MethodFee
                        {
                            Symbol = "ELF",
                            BasicFee = 5000_0000L
                        }
                    }
                });
            var result = await ConfigurationContractStub.GetMethodFee.CallAsync(new StringValue
            {
                Value = nameof(ConfigurationContractStub.SetConfiguration)
            });
            var tokenFee = result.Fees.First();
            tokenFee.Symbol.ShouldBe("ELF");
            tokenFee.BasicFee.ShouldBe(5000_0000L);
        }

        [Fact]
        public async Task SetBlockTransactionLimit_Test()
        {
            await ExecuteProposalTransaction(Tester, ConfigurationAddress,
                nameof(ConfigurationContractStub.SetConfiguration),
                new SetConfigurationInput
                {
                    Key = BlockTransactionLimitConfigurationNameProvider.Name,
                    Value = new Int32Value
                    {
                        Value = 50
                    }.ToByteString()
                });
            var result = await ConfigurationContractStub.GetConfiguration.CallAsync(new StringValue
            {
                Value = BlockTransactionLimitConfigurationNameProvider.Name
            });
            var limit = new Int32Value();
            limit.MergeFrom(BytesValue.Parser.ParseFrom(result.ToByteString()).Value);
            limit.Value.ShouldBe(50);
        }

        [Fact]
        public async Task Configuration_ChangeMethodFeeController_Test()
        {
            var createOrganizationResult = await ParliamentContractStub.CreateOrganization.SendAsync(
                new CreateOrganizationInput
                {
                    ProposalReleaseThreshold = new ProposalReleaseThreshold
                    {
                        MinimalApprovalThreshold = 1000,
                        MinimalVoteThreshold = 1000
                    }
                });

            var organizationAddress = createOrganizationResult.Output;

            var methodFeeController = await ConfigurationContractStub.GetMethodFeeController.CallAsync(new Empty());
            await ExecuteProposalTransaction(Tester, ConfigurationAddress,
                nameof(ConfigurationContractStub.ChangeMethodFeeController),
                new AuthorityInfo
                {
                    OwnerAddress = organizationAddress,
                    ContractAddress = methodFeeController.ContractAddress
                });

            var newMethodFeeController = await ConfigurationContractStub.GetMethodFeeController.CallAsync(new Empty());
            newMethodFeeController.OwnerAddress.ShouldBe(organizationAddress);
        }

        [Fact]
        public async Task ChangeOwnerAddress_Test()
        {
            var defaultOrganization = await ParliamentContractStub.GetDefaultOrganizationAddress.CallAsync(new Empty());
            var defaultOwner = await ConfigurationContractStub.GetConfigurationController.CallAsync(new Empty());
            defaultOwner.ShouldBe(defaultOrganization);

            await ExecuteProposalTransaction(Tester, ConfigurationAddress,
                nameof(ConfigurationContractStub.ChangeConfigurationController),
                Tester);
            
            var newOwner = await ConfigurationContractStub.GetConfigurationController.CallAsync(new Empty());
            newOwner.ShouldBe(Tester);
        }
    }
}