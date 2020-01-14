using System.Collections.Generic;
using System.Threading.Tasks;
using Acs6;
using AElf.Types;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Shouldly;
using Xunit;

namespace AElf.Contracts.AEDPoSExtension.Demo.Tests
{
    public class RandomNumberGenerateTests : AEDPoSExtensionDemoTestBase
    {
        [Fact]
        public async Task RequestRandomNumberTest()
        {
            await BlockMiningService.MineBlockToNextRoundAsync();
            await BlockMiningService.MineBlockToNextRoundAsync();
            await BlockMiningService.MineBlockToNextRoundAsync();
            await BlockMiningService.MineBlockToNextRoundAsync();
            await BlockMiningService.MineBlockToNextRoundAsync();
            await BlockMiningService.MineBlockToNextRoundAsync();
            var requestTx = ConsensusStub.RequestRandomNumber.GetTransaction(Hash.Empty);
            var refBlockNumber = requestTx.RefBlockNumber;
            await BlockMiningService.MineBlockAsync(new List<Transaction>
            {
                requestTx
            });
            var requestTrace = TransactionTraceProvider.GetTransactionTrace(requestTx.GetHash());
            var randomNumberOrder = new RandomNumberOrder();
            randomNumberOrder.MergeFrom(requestTrace.ReturnValue);
            var targetBlockNumber = randomNumberOrder.BlockHeight;

            targetBlockNumber.ShouldBeLessThan(refBlockNumber + 100);
        }
    }
}