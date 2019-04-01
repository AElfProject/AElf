using System.Linq;
using System.Threading.Tasks;
using AElf.Common;
using AElf.Consensus.DPoS;
using AElf.Cryptography.SecretSharing;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Shouldly;
using Xunit;

namespace AElf.Contracts.Consensus.DPoS
{
    /// <summary>
    /// BTW, we can test `GetInformationToUpdateConsensus`.
    /// </summary>
    public class InValueRecoveryTest : DPoSTestBase
    {
        public int MinimumCount => (int) (MinersCount * 2d / 3);
        
        [Fact]
        public async Task GenerateEncryptedMessagesTest()
        {
            var firstRound = await BootMiner.GetCurrentRoundInformation.CallAsync(new Empty());

            var randomHashes = Enumerable.Range(0, MinersCount).Select(_ => Hash.Generate()).ToList();
            var triggers = Enumerable.Range(0, MinersCount).Select(i => new DPoSTriggerInformation
            {
                PublicKey = ByteString.CopyFrom(InitialMinersKeyPairs[i].PublicKey),
                RandomHash = randomHashes[i]
            }).ToDictionary(t => t.PublicKey.ToHex(), t => t);

            foreach (var minerInRound in firstRound.RealTimeMinersInformation.Values.OrderBy(m => m.Order))
            {
                var currentKeyPair = InitialMinersKeyPairs.First(p => p.PublicKey.ToHex() == minerInRound.PublicKey);

                ECKeyPairProvider.SetECKeyPair(currentKeyPair);

                BlockTimeProvider.SetBlockTime(
                    BlockchainStartTime.AddMilliseconds(minerInRound.Order * MiningInterval));

                var tester = GetConsensusContractTester(currentKeyPair);
                var headerInformation =
                    await tester.GetInformationToUpdateConsensus.CallAsync(triggers[minerInRound.PublicKey]);
                var encryptedInValues = headerInformation.Round.RealTimeMinersInformation[minerInRound.PublicKey]
                    .EncryptedInValues;

                encryptedInValues.Count.ShouldBe(MinersCount);
                foreach (var (key, value) in encryptedInValues)
                {
                    InitialMinersKeyPairs.Select(p => p.PublicKey.ToHex()).ShouldContain(key);
                    value.ShouldNotBeEmpty();
                }

                // Update consensus information to make sure next miner can mine his block.
                var toUpdate = headerInformation.Round.ExtractInformationToUpdateConsensus(minerInRound.PublicKey);
                await tester.UpdateValue.SendAsync(toUpdate);
            }
            
            var updatedRound = await BootMiner.GetCurrentRoundInformation.CallAsync(new Empty());

            foreach (var minerInRound in updatedRound.RealTimeMinersInformation.Values)
            {
                minerInRound.EncryptedInValues.Count.ShouldBe(MinersCount);
            }
        }
    }
}