using System.Linq;
using System.Threading.Tasks;
using AElf.Kernel;
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
        [Fact]
        internal async Task<Hash> AEDPoSContract_RequestRandomNumber()
        {
            var randomNumberOrder = (await AEDPoSContractStub.RequestRandomNumber.SendAsync(new Empty())).Output;
            randomNumberOrder.TokenHash.ShouldNotBeNull();
            randomNumberOrder.BlockHeight.ShouldBeGreaterThan(
                AEDPoSContractTestConstants.InitialMinersCount.Mul(AEDPoSContractTestConstants.TinySlots));
            return randomNumberOrder.TokenHash;
        }

        [Fact]
        internal async Task AEDPoSContract_GetRandomNumber()
        {
            var tokenHash = await AEDPoSContract_RequestRandomNumber();

            var currentRound = await BootMiner.GetCurrentRoundInformation.CallAsync(new Empty());

            var randomHashes = Enumerable.Range(0, AEDPoSContractTestConstants.InitialMinersCount).Select(_ => Hash.Generate()).ToList();
            var triggers = Enumerable.Range(0, AEDPoSContractTestConstants.InitialMinersCount).Select(i => new AElfConsensusTriggerInformation
            {
                PublicKey = ByteString.CopyFrom(InitialMinersKeyPairs[i].PublicKey),
                RandomHash = randomHashes[i]
            }).ToDictionary(t => t.PublicKey.ToHex(), t => t);

            foreach (var minerInRound in currentRound.RealTimeMinersInformation.Values.OrderBy(m => m.Order))
            {
                var currentKeyPair = InitialMinersKeyPairs.First(p => p.PublicKey.ToHex() == minerInRound.PublicKey);

                KeyPairProvider.SetKeyPair(currentKeyPair);

                BlockTimeProvider.SetBlockTime(minerInRound.ExpectedMiningTime);

                var tester = GetAEDPoSContractStub(currentKeyPair);
                var headerInformation =
                    (await tester.GetInformationToUpdateConsensus.CallAsync(triggers[minerInRound.PublicKey]
                        .ToBytesValue())).ToConsensusHeaderInformation();

                // Update consensus information.
                var toUpdate = headerInformation.Round.ExtractInformationToUpdateConsensus(minerInRound.PublicKey);
                await tester.UpdateValue.SendAsync(toUpdate);

                for (var i = 0; i < 8; i++)
                {
                    await tester.UpdateTinyBlockInformation.SendAsync(new TinyBlockInput
                    {
                        ActualMiningTime = TimestampHelper.GetUtcNow(),
                        RoundId = currentRound.RoundId
                    });
                }
            }

            var randomNumber = (await AEDPoSContractStub.GetRandomNumber.SendAsync(tokenHash)).Output;
            randomNumber.ShouldNotBeNull();
        }
    }
}