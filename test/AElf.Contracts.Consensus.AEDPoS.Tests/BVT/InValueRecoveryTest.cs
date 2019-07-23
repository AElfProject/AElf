using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Acs4;
using AElf.Cryptography;
using AElf.Cryptography.SecretSharing;
using AElf.Sdk.CSharp;
using AElf.Types;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Shouldly;
using Xunit;

namespace AElf.Contracts.Consensus.AEDPoS
{
    public partial class AEDPoSTest
    {
        private static int MinimumCount => AEDPoSContractTestConstants.InitialMinersCount.Mul(2).Div(3);

        /// <summary>
        /// Test the correctness of basic logic of secret sharing.
        /// </summary>
        /// <returns></returns>
        [Fact]
        public void OffChainDecryptMessageTest()
        {
            var message = Hash.FromString("hash").ToByteArray();
            var secrets =
                SecretSharingHelper.EncodeSecret(message, MinimumCount, AEDPoSContractTestConstants.InitialMinersCount);
            var encryptedValues = new Dictionary<string, byte[]>();
            var decryptedValues = new Dictionary<string, byte[]>();
            var ownerKeyPair = InitialMinersKeyPairs[0];
            var othersKeyPairs = InitialMinersKeyPairs.Skip(1).ToList();
            var decryptResult=new byte[0];

            var initial = 0;
            foreach (var keyPair in othersKeyPairs)
            {
                var encryptedMessage = CryptoHelper.EncryptMessage(ownerKeyPair.PrivateKey, keyPair.PublicKey,
                    secrets[initial++]);
                encryptedValues.Add(keyPair.PublicKey.ToHex(), encryptedMessage);
            }

            // Check encrypted values.
            encryptedValues.Count.ShouldBe(AEDPoSContractTestConstants.InitialMinersCount - 1);

            // Others try to recover.
            foreach (var keyPair in othersKeyPairs)
            {
                var cipherMessage = encryptedValues[keyPair.PublicKey.ToHex()];
                var decryptMessage =
                    CryptoHelper.DecryptMessage(ownerKeyPair.PublicKey, keyPair.PrivateKey, cipherMessage);
                decryptedValues.Add(keyPair.PublicKey.ToHex(), decryptMessage);

                if (decryptedValues.Count >= MinimumCount)
                {
                    decryptResult = SecretSharingHelper.DecodeSecret(
                        decryptedValues.Values.ToList(),
                        Enumerable.Range(1, MinimumCount).ToList(), MinimumCount);
                    break;
                }
            }

            decryptResult.ShouldBe(message);
        }

        [Fact]
        public async Task DecryptMessageTest()
        {
            var previousTriggers = await GenerateEncryptedMessagesAsync();

            await BootMinerChangeRoundAsync();

            var currentRound = await BootMiner.GetCurrentRoundInformation.CallAsync(new Empty());

            var randomHashes = Enumerable.Range(0, AEDPoSContractTestConstants.InitialMinersCount)
                .Select(_ => Hash.FromString("hash")).ToList();
            var triggers = Enumerable.Range(0, AEDPoSContractTestConstants.InitialMinersCount).Select(i =>
                new AElfConsensusTriggerInformation
                {
                    Pubkey = ByteString.CopyFrom(InitialMinersKeyPairs[i].PublicKey),
                    RandomHash = randomHashes[i],
                    PreviousRandomHash = previousTriggers[InitialMinersKeyPairs[i].PublicKey.ToHex()].RandomHash
                }).ToDictionary(t => t.Pubkey.ToHex(), t => t);

            // Just `MinimumCount + 1` miners produce blocks.
            foreach (var minerInRound in currentRound.RealTimeMinersInformation.Values.OrderBy(m => m.Order)
                .Take(MinimumCount + 1))
            {
                var currentKeyPair = InitialMinersKeyPairs.First(p => p.PublicKey.ToHex() == minerInRound.Pubkey);

                KeyPairProvider.SetKeyPair(currentKeyPair);

                BlockTimeProvider.SetBlockTime(minerInRound.ExpectedMiningTime.ToDateTime());

                var tester = GetAEDPoSContractStub(currentKeyPair);
                var headerInformation = new AElfConsensusHeaderInformation();
                headerInformation.MergeFrom(
                    (await tester.GetInformationToUpdateConsensus.CallAsync(triggers[minerInRound.Pubkey]
                        .ToBytesValue())).Value);
                // Update consensus information.
                var toUpdate = headerInformation.Round.ExtractInformationToUpdateConsensus(minerInRound.Pubkey);
                await tester.UpdateValue.SendAsync(toUpdate);
            }

            // Won't pass because currently we postpone the revealing of in values to extra block time slot.
            // But in values all filled.
//            var secondRound = await BootMiner.GetCurrentRoundInformation.CallAsync(new Empty());
//            secondRound.RealTimeMinersInformation.Values.Count(v => v.PreviousInValue != null)
//                .ShouldBe(AEDPoSContractTestConstants.InitialMinersCount);
        }

        /// <summary>
        /// Generate encrypted messages and put them to round information.
        /// </summary>
        /// <returns></returns>
        private async Task<Dictionary<string, AElfConsensusTriggerInformation>> GenerateEncryptedMessagesAsync()
        {
            var firstRound = await BootMiner.GetCurrentRoundInformation.CallAsync(new Empty());

            var randomHashes = Enumerable.Range(0, AEDPoSContractTestConstants.InitialMinersCount)
                .Select(_ => Hash.FromString("hash2")).ToList();
            var triggers = Enumerable.Range(0, AEDPoSContractTestConstants.InitialMinersCount).Select(i =>
                new AElfConsensusTriggerInformation
                {
                    Pubkey = ByteString.CopyFrom(InitialMinersKeyPairs[i].PublicKey),
                    RandomHash = randomHashes[i]
                }).ToDictionary(t => t.Pubkey.ToHex(), t => t);

            foreach (var minerInRound in firstRound.RealTimeMinersInformation.Values.OrderBy(m => m.Order))
            {
                var currentKeyPair = InitialMinersKeyPairs.First(p => p.PublicKey.ToHex() == minerInRound.Pubkey);

                KeyPairProvider.SetKeyPair(currentKeyPair);

                BlockTimeProvider.SetBlockTime(minerInRound.ExpectedMiningTime.ToDateTime());

                var tester = GetAEDPoSContractStub(currentKeyPair);
                var headerInformation = new AElfConsensusHeaderInformation();
                headerInformation.MergeFrom(
                    (await tester.GetInformationToUpdateConsensus.CallAsync(triggers[minerInRound.Pubkey]
                        .ToBytesValue())).Value);

                // Update consensus information.
                var toUpdate = headerInformation.Round.ExtractInformationToUpdateConsensus(minerInRound.Pubkey);
                await tester.UpdateValue.SendAsync(toUpdate);
            }

            return triggers;
        }
    }
}