using System.Threading.Tasks;
using AElf.Kernel;
using AElf.Types;
using AElf.Contracts.Configuration;
using Google.Protobuf.WellKnownTypes;
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

            var oldLimit = BlockTransactionLimitChanged.Parser.ParseFrom(transactionResult.Logs[0].NonIndexed).Old;
            var newLimit = BlockTransactionLimitChanged.Parser.ParseFrom(transactionResult.Logs[0].NonIndexed).New;

            Assert.True(oldLimit == 0);
            Assert.True(newLimit == 100);
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
                    nameof(ConfigurationContainer.ConfigurationStub.GetOwnerAddress),
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
                    nameof(ConfigurationContainer.ConfigurationStub.ChangeOwnerAddress),
                    SampleAddress.AddressList[0]);
            var status = transactionResult.Status;
            Assert.True(status == TransactionResultStatus.Failed);
            Assert.Contains("Not authorized to do this.", transactionResult.Error);
        }
    }
}