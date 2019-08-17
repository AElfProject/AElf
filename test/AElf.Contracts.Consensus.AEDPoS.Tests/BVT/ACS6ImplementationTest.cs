using System.Linq;
using System.Threading.Tasks;
using Acs6;
using AElf.Contracts.Economic.TestBase;
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
        internal async Task<Hash> AEDPoSContract_RequestRandomNumber_Test()
        {
            var randomNumberOrder =
                (await AEDPoSContractStub.RequestRandomNumber.SendAsync(new RequestRandomNumberInput())).Output;
            randomNumberOrder.TokenHash.ShouldNotBeNull();
            randomNumberOrder.BlockHeight.ShouldBeGreaterThan(
                AEDPoSContractTestConstants.InitialMinersCount.Mul(AEDPoSContractTestConstants.TinySlots));
            return randomNumberOrder.TokenHash;
        }

        [Fact]
        internal async Task<Hash> AEDPoSContract_RequestRandomNumber_FillMinimumBlockHeight_Test()
        {
            const long minimumBlockHeight = 1000;
            var randomNumberOrder = (await AEDPoSContractStub.RequestRandomNumber.SendAsync(new RequestRandomNumberInput
            {
                MinimumBlockHeight = minimumBlockHeight
            })).Output;
            randomNumberOrder.TokenHash.ShouldNotBeNull();
            randomNumberOrder.BlockHeight.ShouldBe(minimumBlockHeight);
            return randomNumberOrder.TokenHash;
        }

        [Fact]
        public async Task AEDPoSContract_RequestRandomNumber_FillMinimumBlockHeight_MultipleTimes_Test()
        {
            var hash = await AEDPoSContract_RequestRandomNumber_FillMinimumBlockHeight_Test();

            const long minimumBlockHeight = 1000;
            var randomNumberOrder = (await AEDPoSContractStub.RequestRandomNumber.SendAsync(new RequestRandomNumberInput
            {
                MinimumBlockHeight = minimumBlockHeight
            })).Output;
            var hash1 = randomNumberOrder.TokenHash;
            hash1.ShouldNotBe(hash);
        }

        [Fact]
        internal async Task<Hash> AEDPoSContract_GetRandomNumber_Test()
        {
            var tokenHash = await AEDPoSContract_RequestRandomNumber_Test();

            var currentRound = await AEDPoSContractStub.GetCurrentRoundInformation.CallAsync(new Empty());

            var randomHashes = Enumerable.Range(0, EconomicContractsTestConstants.InitialCoreDataCenterCount)
                .Select(_ => Hash.FromString("random")).ToList();
            var triggers = Enumerable.Range(0, EconomicContractsTestConstants.InitialCoreDataCenterCount).Select(i =>
                new AElfConsensusTriggerInformation
                {
                    Pubkey = ByteString.CopyFrom(InitialCoreDataCenterKeyPairs[i].PublicKey),
                    RandomHash = randomHashes[i]
                }).ToDictionary(t => t.Pubkey.ToHex(), t => t);

            // Exactly one round except extra block time slot.
            foreach (var minerInRound in currentRound.RealTimeMinersInformation.Values.OrderBy(m => m.Order))
            {
                var currentKeyPair =
                    InitialCoreDataCenterKeyPairs.First(p => p.PublicKey.ToHex() == minerInRound.Pubkey);

                KeyPairProvider.SetKeyPair(currentKeyPair);

                BlockTimeProvider.SetBlockTime(minerInRound.ExpectedMiningTime);

                var stub = GetAEDPoSContractStub(currentKeyPair);
                var headerInformation =
                    (await stub.GetInformationToUpdateConsensus.CallAsync(triggers[minerInRound.Pubkey]
                        .ToBytesValue())).ToConsensusHeaderInformation();

                // Update consensus information.
                var toUpdate = headerInformation.Round.ExtractInformationToUpdateConsensus(minerInRound.Pubkey);
                await stub.UpdateValue.SendAsync(toUpdate);

                // Not enough.
                if (minerInRound.Order < 8)
                {
                    {
                        var transactionResult = (await AEDPoSContractStub.GetRandomNumber.SendAsync(tokenHash))
                            .TransactionResult;
                        transactionResult.Error.ShouldContain("Still preparing random number.");
                    }
                }

                for (var i = 0; i < 7; i++)
                {
                    await stub.UpdateTinyBlockInformation.SendAsync(new TinyBlockInput
                    {
                        ActualMiningTime = TimestampHelper.GetUtcNow(),
                        RoundId = currentRound.RoundId
                    });
                }
            }

            currentRound.GenerateNextRoundInformation(TimestampHelper.GetUtcNow(), BlockchainStartTimestamp,
                out var nextRound);
            await AEDPoSContractStub.NextRound.SendAsync(nextRound);

            // Now it's enough.
            {
                var randomNumber = (await AEDPoSContractStub.GetRandomNumber.SendAsync(tokenHash)).Output;
                randomNumber.Value.ShouldNotBeEmpty();
            }

            // Now we can get this random number again.
            {
                var randomNumber = (await AEDPoSContractStub.GetRandomNumber.SendAsync(tokenHash)).Output;
                randomNumber.Value.ShouldNotBeEmpty();
            }

            return tokenHash;
        }

        [Fact]
        internal async Task AEDPoSContract_GetRandomNumber_AfterSixRounds_Test()
        {
            var tokenHash = await AEDPoSContract_GetRandomNumber_Test();

            // Run 6 rounds
            await RunMiningProcess(6);

            // Should be failed when getting random number for this token.
            {
                var randomNumber = (await AEDPoSContractStub.GetRandomNumber.SendAsync(tokenHash)).Output;
                randomNumber.Value.ShouldBeEmpty();
            }
        }

        private async Task RunMiningProcess(int roundsCount)
        {
            for (var count = 0; count < roundsCount; count++)
            {
                var currentRound = await AEDPoSContractStub.GetCurrentRoundInformation.CallAsync(new Empty());
                var randomHashes = Enumerable.Range(0, EconomicContractsTestConstants.InitialCoreDataCenterCount)
                    .Select(_ => Hash.FromString($"random{count}")).ToList();
                var triggers = Enumerable.Range(0, EconomicContractsTestConstants.InitialCoreDataCenterCount).Select(i =>
                    new AElfConsensusTriggerInformation
                    {
                        Pubkey = ByteString.CopyFrom(InitialCoreDataCenterKeyPairs[i].PublicKey),
                        RandomHash = randomHashes[i]
                    }).ToDictionary(t => t.Pubkey.ToHex(), t => t);

                // Exactly one round except extra block time slot.
                foreach (var minerInRound in currentRound.RealTimeMinersInformation.Values.OrderBy(m => m.Order))
                {
                    var currentKeyPair =
                        InitialCoreDataCenterKeyPairs.First(p => p.PublicKey.ToHex() == minerInRound.Pubkey);

                    KeyPairProvider.SetKeyPair(currentKeyPair);

                    BlockTimeProvider.SetBlockTime(minerInRound.ExpectedMiningTime);

                    var tester = GetAEDPoSContractStub(currentKeyPair);
                    var headerInformation =
                        (await tester.GetInformationToUpdateConsensus.CallAsync(triggers[minerInRound.Pubkey]
                            .ToBytesValue())).ToConsensusHeaderInformation();

                    // Update consensus information.
                    var toUpdate = headerInformation.Round.ExtractInformationToUpdateConsensus(minerInRound.Pubkey);
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

                currentRound.GenerateNextRoundInformation(TimestampHelper.GetUtcNow(), BlockchainStartTimestamp,
                    out var nextRound);
                await AEDPoSContractStub.NextRound.SendAsync(nextRound);
                for (var i = 0; i < 8; i++)
                {
                    await AEDPoSContractStub.UpdateTinyBlockInformation.SendAsync(new TinyBlockInput
                    {
                        ActualMiningTime = TimestampHelper.GetUtcNow(),
                        RoundId = currentRound.RoundId
                    });
                }
            }
        }
    }
}