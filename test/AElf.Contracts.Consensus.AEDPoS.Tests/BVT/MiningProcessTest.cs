using System;
using System.Linq;
using System.Threading.Tasks;
using AElf.Contracts.Election;
using AElf.Contracts.MultiToken.Messages;
using AElf.Cryptography;
using AElf.Types;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Shouldly;
using Volo.Abp.Threading;
using Xunit;
using Xunit.Abstractions;

namespace AElf.Contracts.Consensus.AEDPoS
{
    public class MiningProcessTest : AElfConsensusContractTestBase
    {
        private readonly ITestOutputHelper _testOutputHelper;

        public MiningProcessTest(ITestOutputHelper testOutputHelper)
        {
            _testOutputHelper = testOutputHelper;
            InitializeContracts();
            AsyncHelper.RunSync(() => InitializeCandidates(InitialMinersCount));
            AsyncHelper.RunSync(InitializeVoters);
        }

        [Fact]
        public async Task EvilNodeDetectionTest()
        {
            var firstRound = await BootMiner.GetCurrentRoundInformation.CallAsync(new Empty());

            var randomHashes = Enumerable.Range(0, InitialMinersCount).Select(_ => Hash.Generate()).ToList();
            var triggers = Enumerable.Range(0, InitialMinersCount).Select(i => new AElfConsensusTriggerInformation
            {
                PublicKey = ByteString.CopyFrom(InitialMinersKeyPairs[i].PublicKey),
                RandomHash = randomHashes[i]
            }).ToDictionary(t => t.PublicKey.ToHex(), t => t);

            var voter = GetElectionContractTester(VotersKeyPairs[0]);
            var oneMoreCandidateKeyPair = CryptoHelpers.GenerateKeyPair();
            await GetTokenContractTester(BootMinerKeyPair).Transfer.SendAsync(new TransferInput
            {
                Symbol = "ELF",
                Amount = 10_0000,
                To = Address.FromPublicKey(oneMoreCandidateKeyPair.PublicKey)
            });
            var oneMoreCandidate = GetElectionContractTester(oneMoreCandidateKeyPair);
            await oneMoreCandidate.AnnounceElection.SendAsync(new Empty());

            //in order candidate 0 be selected
            var count = 0;
            foreach (var candidateKeyPair in CandidatesKeyPairs.Take(InitialMinersCount).Append(oneMoreCandidateKeyPair))
            {
                await voter.Vote.SendAsync(new VoteMinerInput
                {
                    CandidatePublicKey = candidateKeyPair.PublicKey.ToHex(),
                    Amount = 10000 - new Random().Next(1, 200) * count,
                    EndTimestamp = DateTime.UtcNow.Add(TimeSpan.FromDays(100)).ToTimestamp()
                });
                count++;
            }

            foreach (var minerInRound in firstRound.RealTimeMinersInformation.Values.OrderBy(m => m.Order))
            {
                var currentKeyPair = InitialMinersKeyPairs.First(p => p.PublicKey.ToHex() == minerInRound.PublicKey);

                ECKeyPairProvider.SetKeyPair(currentKeyPair);

                BlockTimeProvider.SetBlockTime(minerInRound.ExpectedMiningTime.ToDateTime());

                var tester = GetAElfConsensusContractTester(currentKeyPair);
                var headerInformationBytes =
                    await tester.GetInformationToUpdateConsensus.CallAsync(triggers[minerInRound.PublicKey]
                        .ToBytesValue());
                var headerInformation = new AElfConsensusHeaderInformation();
                headerInformation.MergeFrom(headerInformationBytes.Value);

                // Update consensus information.
                var toUpdate = headerInformation.Round.ExtractInformationToUpdateConsensus(minerInRound.PublicKey);
                await tester.UpdateValue.SendAsync(toUpdate);
            }

            var changeTermTime = BlockchainStartTime.AddMinutes(TimeEachTerm + 1);
            BlockTimeProvider.SetBlockTime(changeTermTime);

            var nextTermInformationBytes = await BootMiner.GetInformationToUpdateConsensus.CallAsync(
                new AElfConsensusTriggerInformation
                {
                    Behaviour = AElfConsensusBehaviour.NextTerm,
                    PublicKey = ByteString.CopyFrom(BootMinerKeyPair.PublicKey)
                }.ToBytesValue());
            var nextTermInformation = new AElfConsensusHeaderInformation();
            nextTermInformation.MergeFrom(nextTermInformationBytes.Value);
            await BootMiner.NextTerm.SendAsync(nextTermInformation.Round);

            // First candidate cheat others with in value.
            var oneCandidate = GetAElfConsensusContractTester(CandidatesKeyPairs[0]);
            var anotherCandidate = GetAElfConsensusContractTester(CandidatesKeyPairs[1]);
            var randomHash = Hash.Generate();
            var input = new AElfConsensusTriggerInformation
            {
                Behaviour = AElfConsensusBehaviour.UpdateValue,
                PreviousRandomHash = Hash.Empty,
                RandomHash = randomHash,
                PublicKey = ByteString.CopyFrom(CandidatesKeyPairs[0].PublicKey)
            };
            var informationOfSecondRoundBytes = await oneCandidate.GetInformationToUpdateConsensus.CallAsync(input.ToBytesValue());
            var informationOfSecondRound = new AElfConsensusHeaderInformation();
            informationOfSecondRound.MergeFrom(informationOfSecondRoundBytes.Value);
            if(informationOfSecondRound.Round == null)
                _testOutputHelper.WriteLine(informationOfSecondRound.ToString());
            await oneCandidate.UpdateValue.SendAsync(
                informationOfSecondRound.Round.ExtractInformationToUpdateConsensus(CandidatesKeyPairs[0].PublicKey
                    .ToHex()));

            var thirdRoundStartTime = changeTermTime.AddMinutes(TimeEachTerm + 2);
            BlockTimeProvider.SetBlockTime(thirdRoundStartTime);

            var informationOfThirdRoundBytes = await oneCandidate.GetInformationToUpdateConsensus.CallAsync(
                new AElfConsensusTriggerInformation
                {
                    Behaviour = AElfConsensusBehaviour.NextRound,
                    PublicKey = ByteString.CopyFrom(CandidatesKeyPairs[0].PublicKey)
                }.ToBytesValue());
            var informationOfThirdRound = new AElfConsensusHeaderInformation();
            informationOfThirdRound.MergeFrom(informationOfThirdRoundBytes.Value);
            var thirdRound = informationOfThirdRound.Round;
            await oneCandidate.NextRound.SendAsync(thirdRound);

            var cheatInformationBytes = await oneCandidate.GetInformationToUpdateConsensus.CallAsync(
                new AElfConsensusTriggerInformation
                {
                    Behaviour = AElfConsensusBehaviour.UpdateValue,
                    PreviousRandomHash = Hash.FromMessage(randomHash), // Not same as before.
                    RandomHash = Hash.Generate(), // Don't care this value in current test case.
                    PublicKey = ByteString.CopyFrom(CandidatesKeyPairs[0].PublicKey)
                }.ToBytesValue());
            var cheatInformation = new AElfConsensusHeaderInformation();
            cheatInformation.MergeFrom(cheatInformationBytes.Value);
            await oneCandidate.UpdateValue.SendAsync(
                cheatInformation.Round.ExtractInformationToUpdateConsensus(CandidatesKeyPairs[0].PublicKey.ToHex()));

            // The other miner generate information of next round.
            var fourthRoundStartTime = changeTermTime.AddMinutes(TimeEachTerm + 3);
            BlockTimeProvider.SetBlockTime(fourthRoundStartTime);
            var informationOfFourthRoundBytes = await anotherCandidate.GetInformationToUpdateConsensus.CallAsync(
                new AElfConsensusTriggerInformation
                {
                    Behaviour = AElfConsensusBehaviour.NextRound,
                    PublicKey = ByteString.CopyFrom(CandidatesKeyPairs[1].PublicKey)
                }.ToBytesValue());
            var informationOfFourthRound = new AElfConsensusHeaderInformation();
            informationOfFourthRound.MergeFrom(informationOfFourthRoundBytes.Value);
            var fourthRound = informationOfFourthRound.Round;

            fourthRound.RealTimeMinersInformation.Keys.ShouldNotContain(CandidatesKeyPairs[0].PublicKey.ToHex());
            fourthRound.RealTimeMinersInformation.Keys.ShouldContain(oneMoreCandidateKeyPair.PublicKey.ToHex());
        }

        [Fact]
        public async Task CandidatesNotEnough()
        {
            var firstRound = await BootMiner.GetCurrentRoundInformation.CallAsync(new Empty());

            var randomHashes = Enumerable.Range(0, InitialMinersCount).Select(_ => Hash.Generate()).ToList();
            var triggers = Enumerable.Range(0, InitialMinersCount).Select(i => new AElfConsensusTriggerInformation
            {
                PublicKey = ByteString.CopyFrom(InitialMinersKeyPairs[i].PublicKey),
                RandomHash = randomHashes[i]
            }).ToDictionary(t => t.PublicKey.ToHex(), t => t);

            var voter = GetElectionContractTester(VotersKeyPairs[0]);

            foreach (var candidateKeyPair in CandidatesKeyPairs.Take(InitialMinersCount))
            {
                await voter.Vote.SendAsync(new VoteMinerInput
                {
                    CandidatePublicKey = candidateKeyPair.PublicKey.ToHex(),
                    Amount = 100 + new Random().Next(1, 200),
                    EndTimestamp = DateTime.UtcNow.Add(TimeSpan.FromDays(100)).ToTimestamp()
                });
            }

            foreach (var minerInRound in firstRound.RealTimeMinersInformation.Values.OrderBy(m => m.Order))
            {
                var currentKeyPair = InitialMinersKeyPairs.First(p => p.PublicKey.ToHex() == minerInRound.PublicKey);

                ECKeyPairProvider.SetKeyPair(currentKeyPair);

                BlockTimeProvider.SetBlockTime(minerInRound.ExpectedMiningTime.ToDateTime());

                var tester = GetAElfConsensusContractTester(currentKeyPair);
                var headerInformation =
                    (await tester.GetInformationToUpdateConsensus.CallAsync(triggers[minerInRound.PublicKey]
                        .ToBytesValue())).ToConsensusHeaderInformation();

                // Update consensus information.
                var toUpdate = headerInformation.Round.ExtractInformationToUpdateConsensus(minerInRound.PublicKey);
                await tester.UpdateValue.SendAsync(toUpdate);
            }

            var changeTermTime = BlockchainStartTime.AddMinutes(TimeEachTerm + 1);
            BlockTimeProvider.SetBlockTime(changeTermTime);

            var nextTermInformation = (await BootMiner.GetInformationToUpdateConsensus.CallAsync(
                new AElfConsensusTriggerInformation
                {
                    Behaviour = AElfConsensusBehaviour.NextTerm,
                    PublicKey = ByteString.CopyFrom(BootMinerKeyPair.PublicKey)
                }.ToBytesValue())).ToConsensusHeaderInformation();

            await BootMiner.NextTerm.SendAsync(nextTermInformation.Round);

            // First candidate cheat others with in value.
            var oneCandidate = GetAElfConsensusContractTester(CandidatesKeyPairs[0]);
            var anotherCandidate = GetAElfConsensusContractTester(CandidatesKeyPairs[1]);
            var randomHash = Hash.Generate();
            var informationOfSecondRound = (await oneCandidate.GetInformationToUpdateConsensus.CallAsync(
                new AElfConsensusTriggerInformation
                {
                    Behaviour = AElfConsensusBehaviour.UpdateValue,
                    PreviousRandomHash = Hash.Empty,
                    RandomHash = randomHash,
                    PublicKey = ByteString.CopyFrom(CandidatesKeyPairs[0].PublicKey)
                }.ToBytesValue())).ToConsensusHeaderInformation();
            await oneCandidate.UpdateValue.SendAsync(
                informationOfSecondRound.Round.ExtractInformationToUpdateConsensus(CandidatesKeyPairs[0].PublicKey
                    .ToHex()));

            var thirdRoundStartTime = changeTermTime.AddMinutes(TimeEachTerm + 2);
            BlockTimeProvider.SetBlockTime(thirdRoundStartTime);
            var thirdRound = ((await oneCandidate.GetInformationToUpdateConsensus.CallAsync(new AElfConsensusTriggerInformation
            {
                Behaviour = AElfConsensusBehaviour.NextRound,
                PublicKey = ByteString.CopyFrom(CandidatesKeyPairs[0].PublicKey)
            }.ToBytesValue()))).ToConsensusHeaderInformation().Round;

            await oneCandidate.NextRound.SendAsync(thirdRound);

            var cheatInformation = (await oneCandidate.GetInformationToUpdateConsensus.CallAsync(
                new AElfConsensusTriggerInformation
                {
                    Behaviour = AElfConsensusBehaviour.UpdateValue,
                    PreviousRandomHash = Hash.FromMessage(randomHash), // Not same as before.
                    RandomHash = Hash.Generate(), // Don't care this value in current test case.
                    PublicKey = ByteString.CopyFrom(CandidatesKeyPairs[0].PublicKey)
                }.ToBytesValue())).ToConsensusHeaderInformation();
            await oneCandidate.UpdateValue.SendAsync(
                cheatInformation.Round.ExtractInformationToUpdateConsensus(CandidatesKeyPairs[0].PublicKey.ToHex()));

            // The other miner generate information of next round.
            var fourthRoundStartTime = changeTermTime.AddMinutes(TimeEachTerm + 3);
            BlockTimeProvider.SetBlockTime(fourthRoundStartTime);
            var fourthRound = ((await anotherCandidate.GetInformationToUpdateConsensus.CallAsync(
                new AElfConsensusTriggerInformation
                {
                    Behaviour = AElfConsensusBehaviour.NextRound,
                    PublicKey = ByteString.CopyFrom(CandidatesKeyPairs[1].PublicKey)
                }.ToBytesValue()))).ToConsensusHeaderInformation().Round;

            fourthRound.RealTimeMinersInformation.Keys.ShouldNotContain(CandidatesKeyPairs[0].PublicKey.ToHex());
            var newMiner = fourthRound.RealTimeMinersInformation.Keys.Where(k => !thirdRound.RealTimeMinersInformation.ContainsKey(k))
                .ToList()[0];
            firstRound.RealTimeMinersInformation.Keys.ToList().ShouldContain(newMiner);
        }
    }
}