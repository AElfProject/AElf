using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Cryptography;
using AElf.Cryptography.ECDSA;
using Xunit;

namespace AElf.Kernel.Consensus.DPoS.Tests
{
    public class ConsensusCommonTest
    {
        [Fact]
        public async Task TriggerConsensusTest_BootMiner()
        {
            var initialMinersKeyPairs = ConsensusTestHelper.GenerateMinersKeyPairs(17);

            var tester = new ConsensusTester(0, CryptoHelpers.GenerateKeyPair(), initialMinersKeyPairs, true);

            await tester.TriggerConsensusAsync();

            Assert.True(tester.ScheduleTriggered);
        }
        
        [Fact]
        public async Task TriggerConsensusTest_NotBootMiner()
        {
            var initialMinersKeyPairs = ConsensusTestHelper.GenerateMinersKeyPairs(17);

            var tester = new ConsensusTester(0, CryptoHelpers.GenerateKeyPair(), initialMinersKeyPairs);

            await tester.TriggerConsensusAsync();

            Assert.False(tester.ScheduleTriggered);
        }

        [Fact]
        public async Task GetNewConsensusInformationTest()
        {
            var initialMinersKeyPairs = ConsensusTestHelper.GenerateMinersKeyPairs(17);

            var tester = new ConsensusTester(0, CryptoHelpers.GenerateKeyPair(), initialMinersKeyPairs);

            await tester.TriggerConsensusAsync();
            
            await tester.GenerateConsensusTransactionsAsync();

            var consensusInformation = await tester.GetNewConsensusInformationAsync();

            Assert.NotNull(consensusInformation);
        }

        [Fact]
        public async Task GenerateConsensusTransactionsTest()
        {
            var initialMinersKeyPairs = ConsensusTestHelper.GenerateMinersKeyPairs(17);

            var tester = new ConsensusTester(0, CryptoHelpers.GenerateKeyPair(), initialMinersKeyPairs);

            await tester.TriggerConsensusAsync();

            var txs = await tester.GenerateConsensusTransactionsAsync();

            Assert.True(txs.Any());
        }

        [Fact]
        public async Task ValidateConsensusBeforeExecutionTest()
        {
            var tester = ConsensusTestHelper.CreateABootMiner();

            await tester.TriggerConsensusAsync();
            
            await tester.GenerateConsensusTransactionsAsync();

            var consensusInformation = await tester.GetNewConsensusInformationAsync();

            var validateResult = await tester.ValidateConsensusBeforeExecutionAsync(consensusInformation);

            Assert.True(validateResult);
        }
        
        [Fact]
        public async Task ValidateConsensusAfterExecutionTest()
        {
            var tester = ConsensusTestHelper.CreateABootMiner();

            await tester.TriggerConsensusAsync();
            
            await tester.GenerateConsensusTransactionsAsync();

            var consensusInformation = await tester.GetNewConsensusInformationAsync();

            var validateResult = await tester.ValidateConsensusAfterExecutionAsync(consensusInformation);

            Assert.True(validateResult);
        }
    }
}