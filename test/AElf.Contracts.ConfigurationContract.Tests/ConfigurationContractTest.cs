using System.Threading.Tasks;
using AElf.Types;
using Configuration;
using Google.Protobuf.WellKnownTypes;
using Xunit;

namespace AElf.Contracts.ConfigurationContract.Tests
{
    public class ConfigurationContractTest : ConfigurationContractTestBase
    {
        [Fact]
        public async Task Set_Block_Transaction_Limit()
        {
            var proposalId = await SetBlockTransactionLimitProposalAsync(100);
            await ApproveWithMinersAsync(proposalId);
            var transactionResult = await ReleaseProposalAsync(proposalId);

            var oldLimit = BlockTransactionLimitChanged.Parser.ParseFrom(transactionResult.Logs[0].NonIndexed).Old;
            var newLimit = BlockTransactionLimitChanged.Parser.ParseFrom(transactionResult.Logs[0].NonIndexed).New;

            Assert.True(newLimit == 100);
        }

        [Fact]
        public async Task Set_Block_Transaction_Limit__NotAuthorized()
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
    }
}