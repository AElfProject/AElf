using System;
using System.Linq;
using System.Threading.Tasks;
using Acs4;
using AElf.Contracts.MultiToken.Messages;
using AElf.Cryptography;
using AElf.Types;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Shouldly;
using Volo.Abp.Threading;
using Xunit;
using Xunit.Abstractions;

namespace AElf.Contracts.Consensus.DPoS
{
    public class MiningProcessTest : DPoSTestBase
    {
        private readonly ITestOutputHelper _testOutputHelper;

        public MiningProcessTest(ITestOutputHelper testOutputHelper)
        {
            _testOutputHelper = testOutputHelper;
            InitializeContracts();
            AsyncHelper.RunSync(() => InitializeCandidates(MinersCount));
            AsyncHelper.RunSync(InitializeVoters);
        }

        [Fact]
        public async Task EvilNodeDetectionTest()
        {
            var firstRound = await BootMiner.GetCurrentRoundInformation.CallAsync(new Empty());

            var randomHashes = Enumerable.Range(0, MinersCount).Select(_ => Hash.Generate()).ToList();
            var triggers = Enumerable.Range(0, MinersCount).Select(i => new DPoSTriggerInformation
            {
                PublicKey = ByteString.CopyFrom(InitialMinersKeyPairs[i].PublicKey),
                RandomHash = randomHashes[i]
            }).ToDictionary(t => t.PublicKey.ToHex(), t => t);

            var voter = GetConsensusContractTester(VotersKeyPairs[0]);
            var oneMoreCandidateKeyPair = CryptoHelpers.GenerateKeyPair();
            await GetTokenContractTester(BootMinerKeyPair).Transfer.SendAsync(new TransferInput
            {
                Symbol = "ELF",
                Amount = 10_0000,
                To = Address.FromPublicKey(oneMoreCandidateKeyPair.PublicKey)
            });
            var oneMoreCandidate = GetConsensusContractTester(oneMoreCandidateKeyPair);
            await oneMoreCandidate.AnnounceElection.SendAsync(new Alias
            {
                Value = oneMoreCandidateKeyPair.PublicKey.ToHex().Substring(0, 20)
            });

            //in order candidate 0 be selected
            var count = 0;
            foreach (var candidateKeyPair in CandidatesKeyPairs.Take(MinersCount).Append(oneMoreCandidateKeyPair))
            {
                await voter.Vote.SendAsync(new VoteInput
                {
                    CandidatePublicKey = candidateKeyPair.PublicKey.ToHex(),
                    Amount = 10000 - new Random().Next(1, 200) * count,
                    LockTime = 100
                });
                count++;
            }

            foreach (var minerInRound in firstRound.RealTimeMinersInformation.Values.OrderBy(m => m.Order))
            {
                var currentKeyPair = InitialMinersKeyPairs.First(p => p.PublicKey.ToHex() == minerInRound.PublicKey);

                ECKeyPairProvider.SetKeyPair(currentKeyPair);

                BlockTimeProvider.SetBlockTime(minerInRound.ExpectedMiningTime.ToDateTime());

                var tester = GetConsensusContractTester(currentKeyPair);
                var headerInformation =
                    await tester.GetInformationToUpdateConsensus.CallAsync(triggers[minerInRound.PublicKey]);

                // Update consensus information.
                var toUpdate = headerInformation.Round.ExtractInformationToUpdateConsensus(minerInRound.PublicKey);
                await tester.UpdateValue.SendAsync(toUpdate);
            }

            var changeTermTime = BlockchainStartTime.AddMinutes(ConsensusDPoSConsts.DaysEachTerm + 1);
            BlockTimeProvider.SetBlockTime(changeTermTime);

            var nextTermInformation = await BootMiner.GetInformationToUpdateConsensus.CallAsync(
                new DPoSTriggerInformation
                {
                    Behaviour = DPoSBehaviour.NextTerm,
                    PublicKey = ByteString.CopyFrom(BootMinerKeyPair.PublicKey)
                });

            await BootMiner.NextTerm.SendAsync(nextTermInformation.Round);

            // First candidate cheat others with in value.
            var oneCandidate = GetConsensusContractTester(CandidatesKeyPairs[0]);
            var anotherCandidate = GetConsensusContractTester(CandidatesKeyPairs[1]);
            var randomHash = Hash.Generate();
            var input = new DPoSTriggerInformation
            {
                Behaviour = DPoSBehaviour.UpdateValue,
                PreviousRandomHash = Hash.Empty,
                RandomHash = randomHash,
                PublicKey = ByteString.CopyFrom(CandidatesKeyPairs[0].PublicKey)
            };
            var informationOfSecondRound = await oneCandidate.GetInformationToUpdateConsensus.CallAsync(input);
            if(informationOfSecondRound.Round == null)
                _testOutputHelper.WriteLine(informationOfSecondRound.ToString());
            await oneCandidate.UpdateValue.SendAsync(
                informationOfSecondRound.Round.ExtractInformationToUpdateConsensus(CandidatesKeyPairs[0].PublicKey
                    .ToHex()));

            var thirdRoundStartTime = changeTermTime.AddMinutes(ConsensusDPoSConsts.DaysEachTerm + 2);
            BlockTimeProvider.SetBlockTime(thirdRoundStartTime);
            var thirdRound = (await oneCandidate.GetInformationToUpdateConsensus.CallAsync(new DPoSTriggerInformation
            {
                Behaviour = DPoSBehaviour.NextRound,
                PublicKey = ByteString.CopyFrom(CandidatesKeyPairs[0].PublicKey)
            })).Round;

            await oneCandidate.NextRound.SendAsync(thirdRound);

            var cheatInformation = await oneCandidate.GetInformationToUpdateConsensus.CallAsync(
                new DPoSTriggerInformation
                {
                    Behaviour = DPoSBehaviour.UpdateValue,
                    PreviousRandomHash = Hash.FromMessage(randomHash), // Not same as before.
                    RandomHash = Hash.Generate(), // Don't care this value in current test case.
                    PublicKey = ByteString.CopyFrom(CandidatesKeyPairs[0].PublicKey)
                });
            await oneCandidate.UpdateValue.SendAsync(
                cheatInformation.Round.ExtractInformationToUpdateConsensus(CandidatesKeyPairs[0].PublicKey.ToHex()));

            // The other miner generate information of next round.
            var fourthRoundStartTime = changeTermTime.AddMinutes(ConsensusDPoSConsts.DaysEachTerm + 3);
            BlockTimeProvider.SetBlockTime(fourthRoundStartTime);
            var fourthRound = (await anotherCandidate.GetInformationToUpdateConsensus.CallAsync(
                new DPoSTriggerInformation
                {
                    Behaviour = DPoSBehaviour.NextRound,
                    PublicKey = ByteString.CopyFrom(CandidatesKeyPairs[1].PublicKey)
                })).Round;

            fourthRound.RealTimeMinersInformation.Keys.ShouldNotContain(CandidatesKeyPairs[0].PublicKey.ToHex());
            fourthRound.RealTimeMinersInformation.Keys.ShouldContain(oneMoreCandidateKeyPair.PublicKey.ToHex());
        }

        [Fact]
        public async Task CandidatesNotEnough()
        {
            var firstRound = await BootMiner.GetCurrentRoundInformation.CallAsync(new Empty());

            var randomHashes = Enumerable.Range(0, MinersCount).Select(_ => Hash.Generate()).ToList();
            var triggers = Enumerable.Range(0, MinersCount).Select(i => new DPoSTriggerInformation
            {
                PublicKey = ByteString.CopyFrom(InitialMinersKeyPairs[i].PublicKey),
                RandomHash = randomHashes[i]
            }).ToDictionary(t => t.PublicKey.ToHex(), t => t);

            var voter = GetConsensusContractTester(VotersKeyPairs[0]);

            foreach (var candidateKeyPair in CandidatesKeyPairs.Take(MinersCount))
            {
                await voter.Vote.SendAsync(new VoteInput
                {
                    CandidatePublicKey = candidateKeyPair.PublicKey.ToHex(),
                    Amount = 100 + new Random().Next(1, 200),
                    LockTime = 100
                });
            }

            foreach (var minerInRound in firstRound.RealTimeMinersInformation.Values.OrderBy(m => m.Order))
            {
                var currentKeyPair = InitialMinersKeyPairs.First(p => p.PublicKey.ToHex() == minerInRound.PublicKey);

                ECKeyPairProvider.SetKeyPair(currentKeyPair);

                BlockTimeProvider.SetBlockTime(minerInRound.ExpectedMiningTime.ToDateTime());

                var tester = GetConsensusContractTester(currentKeyPair);
                var headerInformation =
                    await tester.GetInformationToUpdateConsensus.CallAsync(triggers[minerInRound.PublicKey]);

                // Update consensus information.
                var toUpdate = headerInformation.Round.ExtractInformationToUpdateConsensus(minerInRound.PublicKey);
                await tester.UpdateValue.SendAsync(toUpdate);
            }

            var changeTermTime = BlockchainStartTime.AddMinutes(ConsensusDPoSConsts.DaysEachTerm + 1);
            BlockTimeProvider.SetBlockTime(changeTermTime);

            var nextTermInformation = await BootMiner.GetInformationToUpdateConsensus.CallAsync(
                new DPoSTriggerInformation
                {
                    Behaviour = DPoSBehaviour.NextTerm,
                    PublicKey = ByteString.CopyFrom(BootMinerKeyPair.PublicKey)
                });

            await BootMiner.NextTerm.SendAsync(nextTermInformation.Round);

            // First candidate cheat others with in value.
            var oneCandidate = GetConsensusContractTester(CandidatesKeyPairs[0]);
            var anotherCandidate = GetConsensusContractTester(CandidatesKeyPairs[1]);
            var randomHash = Hash.Generate();
            var informationOfSecondRound = await oneCandidate.GetInformationToUpdateConsensus.CallAsync(
                new DPoSTriggerInformation
                {
                    Behaviour = DPoSBehaviour.UpdateValue,
                    PreviousRandomHash = Hash.Empty,
                    RandomHash = randomHash,
                    PublicKey = ByteString.CopyFrom(CandidatesKeyPairs[0].PublicKey)
                });
            await oneCandidate.UpdateValue.SendAsync(
                informationOfSecondRound.Round.ExtractInformationToUpdateConsensus(CandidatesKeyPairs[0].PublicKey
                    .ToHex()));

            var thirdRoundStartTime = changeTermTime.AddMinutes(ConsensusDPoSConsts.DaysEachTerm + 2);
            BlockTimeProvider.SetBlockTime(thirdRoundStartTime);
            var thirdRound = (await oneCandidate.GetInformationToUpdateConsensus.CallAsync(new DPoSTriggerInformation
            {
                Behaviour = DPoSBehaviour.NextRound,
                PublicKey = ByteString.CopyFrom(CandidatesKeyPairs[0].PublicKey)
            })).Round;

            await oneCandidate.NextRound.SendAsync(thirdRound);

            var cheatInformation = await oneCandidate.GetInformationToUpdateConsensus.CallAsync(
                new DPoSTriggerInformation
                {
                    Behaviour = DPoSBehaviour.UpdateValue,
                    PreviousRandomHash = Hash.FromMessage(randomHash), // Not same as before.
                    RandomHash = Hash.Generate(), // Don't care this value in current test case.
                    PublicKey = ByteString.CopyFrom(CandidatesKeyPairs[0].PublicKey)
                });
            await oneCandidate.UpdateValue.SendAsync(
                cheatInformation.Round.ExtractInformationToUpdateConsensus(CandidatesKeyPairs[0].PublicKey.ToHex()));

            // The other miner generate information of next round.
            var fourthRoundStartTime = changeTermTime.AddMinutes(ConsensusDPoSConsts.DaysEachTerm + 3);
            BlockTimeProvider.SetBlockTime(fourthRoundStartTime);
            var fourthRound = (await anotherCandidate.GetInformationToUpdateConsensus.CallAsync(
                new DPoSTriggerInformation
                {
                    Behaviour = DPoSBehaviour.NextRound,
                    PublicKey = ByteString.CopyFrom(CandidatesKeyPairs[1].PublicKey)
                })).Round;

            fourthRound.RealTimeMinersInformation.Keys.ShouldNotContain(CandidatesKeyPairs[0].PublicKey.ToHex());
            var newMiner = fourthRound.RealTimeMinersInformation.Keys.Where(k => !thirdRound.RealTimeMinersInformation.ContainsKey(k))
                .ToList()[0];
            firstRound.RealTimeMinersInformation.Keys.ToList().ShouldContain(newMiner);
        }
    }
}