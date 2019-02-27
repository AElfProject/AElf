using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Cryptography;
using AElf.Cryptography.ECDSA;
using Xunit;

namespace AElf.Kernel.Consensus.DPoS.Tests
{
    public class ConsensusServiceTest
    {
        [Fact]
        public async Task TriggerConsensusAsyncTest_InitialTerm()
        {
            var initialMinersKeyPairs = GenerateInitialMiners(17);

            var tester = new ConsensusTester(0, CryptoHelpers.GenerateKeyPair(), initialMinersKeyPairs, true);

            await tester.TriggerConsensusAsync();
            var bytes1 = await tester.GetNewConsensusInformationAsync();

            var result1 = DPoSInformation.Parser.ParseFrom(bytes1);
            
            var validationResult = await tester.ValidateConsensusAsync(bytes1);

            var txs = await tester.GenerateConsensusTransactionsAsync();
        }

        private List<ECKeyPair> GenerateInitialMiners(int minersCount)
        {
            var initialMinersKeyPairs = new List<ECKeyPair>();
            for (var i = 0; i < minersCount; i++)
            {
                initialMinersKeyPairs.Add(CryptoHelpers.GenerateKeyPair());
            }

            return initialMinersKeyPairs;
        }
    }
}