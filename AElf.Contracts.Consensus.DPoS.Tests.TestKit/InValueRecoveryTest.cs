using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AElf.Common;
using AElf.Consensus.DPoS;
using AElf.Cryptography;
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
        private int MinimumCount => (int) (MinersCount * 2d / 3);

        public InValueRecoveryTest()
        {
            InitializeContracts();
        }

        [Fact]
        public async Task<Dictionary<string, DPoSTriggerInformation>> GenerateEncryptedMessagesTest()
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

                BlockTimeProvider.SetBlockTime(minerInRound.ExpectedMiningTime.ToDateTime());

                var tester = GetConsensusContractTester(currentKeyPair);
                var headerInformation =
                    await tester.GetInformationToUpdateConsensus.CallAsync(triggers[minerInRound.PublicKey]);
                var encryptedInValues = headerInformation.Round.RealTimeMinersInformation[minerInRound.PublicKey]
                    .EncryptedInValues;

                encryptedInValues.Count.ShouldBe(MinersCount - 1);
                foreach (var (key, value) in encryptedInValues)
                {
                    InitialMinersKeyPairs.Select(p => p.PublicKey.ToHex()).ShouldContain(key);
                    value.ShouldNotBeEmpty();
                }

                // Update consensus information.
                var toUpdate = headerInformation.Round.ExtractInformationToUpdateConsensus(minerInRound.PublicKey);
                await tester.UpdateValue.SendAsync(toUpdate);
            }

            var updatedRound = await BootMiner.GetCurrentRoundInformation.CallAsync(new Empty());

            foreach (var minerInRound in updatedRound.RealTimeMinersInformation.Values)
            {
                minerInRound.EncryptedInValues.Count.ShouldBe(MinersCount - 1);
            }

            return triggers;
        }

        /// <summary>
        /// Test the correctness of basic logic of secret sharing.
        /// </summary>
        /// <returns></returns>
        [Fact]
        public void OffChainDecryptMessageTest()
        {
            var message = Hash.Generate().ToHex();
            var secrets = SecretSharingHelper.EncodeSecret(message, MinimumCount, MinersCount);
            var encryptedValues = new Dictionary<string, byte[]>();
            var decryptedValues = new Dictionary<string, byte[]>();
            var ownerKeyPair = InitialMinersKeyPairs[0];
            var othersKeyPairs = InitialMinersKeyPairs.Skip(1).ToList();
            var decryptResult = "";

            var initial = 0;
            foreach (var keyPair in othersKeyPairs)
            {
                var encryptedMessage = CryptoHelpers.EncryptMessage(ownerKeyPair.PrivateKey, keyPair.PublicKey,
                    Encoding.UTF8.GetBytes(secrets[initial++]));
                encryptedValues.Add(keyPair.PublicKey.ToHex(), encryptedMessage);
            }

            // Check encrypted values.
            encryptedValues.Count.ShouldBe(MinersCount - 1);

            // Others try to recover.
            foreach (var keyPair in othersKeyPairs)
            {
                var cipherMessage = encryptedValues[keyPair.PublicKey.ToHex()];
                var decryptMessage =
                    CryptoHelpers.DecryptMessage(ownerKeyPair.PublicKey, keyPair.PrivateKey, cipherMessage);
                decryptedValues.Add(keyPair.PublicKey.ToHex(), decryptMessage);

                if (decryptedValues.Count >= MinimumCount)
                {
                    decryptResult = SecretSharingHelper.DecodeSecret(
                        decryptedValues.Values.Select(v => Encoding.UTF8.GetString(v)).ToList(),
                        Enumerable.Range(1, MinimumCount).ToList(), MinimumCount);
                    break;
                }
            }

            decryptResult.ShouldBe(message);
        }

        [Fact]
        public async Task DecryptMessageTest()
        {
            var previousTriggers = await GenerateEncryptedMessagesTest();

            await ChangeRound();

            var currentRound = await BootMiner.GetCurrentRoundInformation.CallAsync(new Empty());

            var randomHashes = Enumerable.Range(0, MinersCount).Select(_ => Hash.Generate()).ToList();
            var triggers = Enumerable.Range(0, MinersCount).Select(i => new DPoSTriggerInformation
            {
                PublicKey = ByteString.CopyFrom(InitialMinersKeyPairs[i].PublicKey),
                RandomHash = randomHashes[i],
                PreviousRandomHash = previousTriggers[InitialMinersKeyPairs[i].PublicKey.ToHex()].RandomHash
            }).ToDictionary(t => t.PublicKey.ToHex(), t => t);

            // Just `MinimumCount + 1` miners produce blocks.
            foreach (var minerInRound in currentRound.RealTimeMinersInformation.Values.OrderBy(m => m.Order)
                .Take(MinimumCount + 1))
            {
                var currentKeyPair = InitialMinersKeyPairs.First(p => p.PublicKey.ToHex() == minerInRound.PublicKey);

                ECKeyPairProvider.SetECKeyPair(currentKeyPair);

                BlockTimeProvider.SetBlockTime(minerInRound.ExpectedMiningTime.ToDateTime());

                var tester = GetConsensusContractTester(currentKeyPair);
                var headerInformation =
                    await tester.GetInformationToUpdateConsensus.CallAsync(triggers[minerInRound.PublicKey]);

                // Update consensus information.
                var toUpdate = headerInformation.Round.ExtractInformationToUpdateConsensus(minerInRound.PublicKey);
                await tester.UpdateValue.SendAsync(toUpdate);
            }

            // But in values all filled.
            var secondRound = await BootMiner.GetCurrentRoundInformation.CallAsync(new Empty());
            secondRound.RealTimeMinersInformation.Values.Count(v => v.PreviousInValue != null).ShouldBe(MinersCount);
        }
    }
}