using System;
using System.Linq;
using System.Threading.Tasks;
using AElf.Contracts.Election;
using AElf.Contracts.MultiToken.Messages;
using AElf.Cryptography;
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
        [Fact(Skip = "This logic can't test evil node detection for now because we improve the validation.")]
        public async Task EvilNodeDetectionTest()
        {
            await InitializeVoters();
            await InitializeCandidates(AEDPoSContractTestConstants.InitialMinersCount);
            
            var firstRound = await BootMiner.GetCurrentRoundInformation.CallAsync(new Empty());

            var randomHashes = Enumerable.Range(0, AEDPoSContractTestConstants.InitialMinersCount).Select(_ => Hash.FromString("hash")).ToList();
            var triggers = Enumerable.Range(0, AEDPoSContractTestConstants.InitialMinersCount).Select(i => new AElfConsensusTriggerInformation
            {
                Pubkey = ByteString.CopyFrom(InitialMinersKeyPairs[i].PublicKey),
                RandomHash = randomHashes[i]
            }).ToDictionary(t => t.Pubkey.ToHex(), t => t);

            var voter = GetElectionContractTester(VotersKeyPairs[0]);
            var oneMoreCandidateKeyPair = CryptoHelper.GenerateKeyPair();
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
            foreach (var candidateKeyPair in CandidatesKeyPairs.Take(AEDPoSContractTestConstants.InitialMinersCount).Append(oneMoreCandidateKeyPair))
            {
                await voter.Vote.SendAsync(new VoteMinerInput
                {
                    CandidatePublicKey = candidateKeyPair.PublicKey.ToHex(),
                    Amount = 10000 - new Random().Next(1, 200) * count,
                    EndTimestamp = TimestampHelper.GetUtcNow().AddDays(100)
                });
                count++;
            }

            foreach (var minerInRound in firstRound.RealTimeMinersInformation.Values.OrderBy(m => m.Order))
            {
                var currentKeyPair = InitialMinersKeyPairs.First(p => p.PublicKey.ToHex() == minerInRound.Pubkey);

                KeyPairProvider.SetKeyPair(currentKeyPair);

                BlockTimeProvider.SetBlockTime(minerInRound.ExpectedMiningTime);

                var tester = GetAEDPoSContractStub(currentKeyPair);
                var headerInformationBytes =
                    await tester.GetInformationToUpdateConsensus.CallAsync(triggers[minerInRound.Pubkey]
                        .ToBytesValue());
                var headerInformation = new AElfConsensusHeaderInformation();
                headerInformation.MergeFrom(headerInformationBytes.Value);

                // Update consensus information.
                var toUpdate = headerInformation.Round.ExtractInformationToUpdateConsensus(minerInRound.Pubkey);
                await tester.UpdateValue.SendAsync(toUpdate);
            }

            var changeTermTime = BlockchainStartTimestamp.ToDateTime().AddMinutes(AEDPoSContractTestConstants.TimeEachTerm + 1);
            BlockTimeProvider.SetBlockTime(changeTermTime);

            var nextTermInformationBytes = await BootMiner.GetInformationToUpdateConsensus.CallAsync(
                new AElfConsensusTriggerInformation
                {
                    Behaviour = AElfConsensusBehaviour.NextTerm,
                    Pubkey = ByteString.CopyFrom(BootMinerKeyPair.PublicKey)
                }.ToBytesValue());
            var nextTermInformation = new AElfConsensusHeaderInformation();
            nextTermInformation.MergeFrom(nextTermInformationBytes.Value);
            await BootMiner.NextTerm.SendAsync(nextTermInformation.Round);

            // First candidate cheat others with in value.
            var oneCandidate = GetAEDPoSContractStub(CandidatesKeyPairs[0]);
            var anotherCandidate = GetAEDPoSContractStub(CandidatesKeyPairs[1]);
            var randomHash = Hash.FromString("hash2");
            var input = new AElfConsensusTriggerInformation
            {
                Behaviour = AElfConsensusBehaviour.UpdateValue,
                PreviousRandomHash = Hash.Empty,
                RandomHash = randomHash,
                Pubkey = ByteString.CopyFrom(CandidatesKeyPairs[0].PublicKey)
            };
            var informationOfSecondRoundBytes = await oneCandidate.GetInformationToUpdateConsensus.CallAsync(input.ToBytesValue());
            var informationOfSecondRound = new AElfConsensusHeaderInformation();
            informationOfSecondRound.MergeFrom(informationOfSecondRoundBytes.Value);
            if(informationOfSecondRound.Round == null)
                _testOutputHelper.WriteLine(informationOfSecondRound.ToString());
            await oneCandidate.UpdateValue.SendAsync(
                informationOfSecondRound.Round.ExtractInformationToUpdateConsensus(CandidatesKeyPairs[0].PublicKey
                    .ToHex()));

            var thirdRoundStartTime = changeTermTime.AddMinutes(AEDPoSContractTestConstants.TimeEachTerm + 2);
            BlockTimeProvider.SetBlockTime(thirdRoundStartTime);

            var informationOfThirdRoundBytes = await oneCandidate.GetInformationToUpdateConsensus.CallAsync(
                new AElfConsensusTriggerInformation
                {
                    Behaviour = AElfConsensusBehaviour.NextRound,
                    Pubkey = ByteString.CopyFrom(CandidatesKeyPairs[0].PublicKey)
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
                    RandomHash = Hash.FromString("hash3"), // Don't care this value in current test case.
                    Pubkey = ByteString.CopyFrom(CandidatesKeyPairs[0].PublicKey)
                }.ToBytesValue());
            var cheatInformation = new AElfConsensusHeaderInformation();
            cheatInformation.MergeFrom(cheatInformationBytes.Value);
            await oneCandidate.UpdateValue.SendAsync(
                cheatInformation.Round.ExtractInformationToUpdateConsensus(CandidatesKeyPairs[0].PublicKey.ToHex()));

            // The other miner generate information of next round.
            var fourthRoundStartTime = changeTermTime.AddMinutes(AEDPoSContractTestConstants.TimeEachTerm + 3);
            BlockTimeProvider.SetBlockTime(fourthRoundStartTime);
            var informationOfFourthRoundBytes = await anotherCandidate.GetInformationToUpdateConsensus.CallAsync(
                new AElfConsensusTriggerInformation
                {
                    Behaviour = AElfConsensusBehaviour.NextRound,
                    Pubkey = ByteString.CopyFrom(CandidatesKeyPairs[1].PublicKey)
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
            await InitializeVoters();
            await InitializeCandidates(AEDPoSContractTestConstants.InitialMinersCount);

            var firstRound = await BootMiner.GetCurrentRoundInformation.CallAsync(new Empty());

            var randomHashes = Enumerable.Range(0, AEDPoSContractTestConstants.InitialMinersCount).Select(_ => Hash.FromString("hash3")).ToList();
            var triggers = Enumerable.Range(0, AEDPoSContractTestConstants.InitialMinersCount).Select(i => new AElfConsensusTriggerInformation
            {
                Pubkey = ByteString.CopyFrom(InitialMinersKeyPairs[i].PublicKey),
                RandomHash = randomHashes[i]
            }).ToDictionary(t => t.Pubkey.ToHex(), t => t);

            var voter = GetElectionContractTester(VotersKeyPairs[0]);

            foreach (var candidateKeyPair in CandidatesKeyPairs.Take(AEDPoSContractTestConstants.InitialMinersCount))
            {
                await voter.Vote.SendAsync(new VoteMinerInput
                {
                    CandidatePublicKey = candidateKeyPair.PublicKey.ToHex(),
                    Amount = 100 + new Random().Next(1, 200),
                    EndTimestamp = TimestampHelper.GetUtcNow().AddDays(100)
                });
            }

            foreach (var minerInRound in firstRound.RealTimeMinersInformation.Values.OrderBy(m => m.Order))
            {
                var currentKeyPair = InitialMinersKeyPairs.First(p => p.PublicKey.ToHex() == minerInRound.Pubkey);

                KeyPairProvider.SetKeyPair(currentKeyPair);

                BlockTimeProvider.SetBlockTime(minerInRound.ExpectedMiningTime);

                var tester = GetAEDPoSContractStub(currentKeyPair);
                var headerInformation =
                    (await tester.GetInformationToUpdateConsensus.CallAsync(triggers[minerInRound.Pubkey]
                        .ToBytesValue())).ToConsensusHeaderInformation();

                // Update consensus information.
                var toUpdate = headerInformation.Round.ExtractInformationToUpdateConsensus(minerInRound.Pubkey);
                await tester.UpdateValue.SendAsync(toUpdate);
            }

            var changeTermTime = BlockchainStartTimestamp.ToDateTime().AddMinutes(AEDPoSContractTestConstants.TimeEachTerm + 1);
            BlockTimeProvider.SetBlockTime(changeTermTime);

            var nextTermInformation = (await BootMiner.GetInformationToUpdateConsensus.CallAsync(
                new AElfConsensusTriggerInformation
                {
                    Behaviour = AElfConsensusBehaviour.NextTerm,
                    Pubkey = ByteString.CopyFrom(BootMinerKeyPair.PublicKey)
                }.ToBytesValue())).ToConsensusHeaderInformation();

            await BootMiner.NextTerm.SendAsync(nextTermInformation.Round);

            // First candidate cheat others with in value.
            var oneCandidate = GetAEDPoSContractStub(CandidatesKeyPairs[0]);
            var anotherCandidate = GetAEDPoSContractStub(CandidatesKeyPairs[1]);
            var randomHash = Hash.FromString("hash5");
            var informationOfSecondRound = (await oneCandidate.GetInformationToUpdateConsensus.CallAsync(
                new AElfConsensusTriggerInformation
                {
                    Behaviour = AElfConsensusBehaviour.UpdateValue,
                    PreviousRandomHash = Hash.Empty,
                    RandomHash = randomHash,
                    Pubkey = ByteString.CopyFrom(CandidatesKeyPairs[0].PublicKey)
                }.ToBytesValue())).ToConsensusHeaderInformation();
            await oneCandidate.UpdateValue.SendAsync(
                informationOfSecondRound.Round.ExtractInformationToUpdateConsensus(CandidatesKeyPairs[0].PublicKey
                    .ToHex()));

            var thirdRoundStartTime = changeTermTime.AddMinutes(AEDPoSContractTestConstants.TimeEachTerm + 2);
            BlockTimeProvider.SetBlockTime(thirdRoundStartTime);
            var thirdRound = (await oneCandidate.GetInformationToUpdateConsensus.CallAsync(new AElfConsensusTriggerInformation
            {
                Behaviour = AElfConsensusBehaviour.NextRound,
                Pubkey = ByteString.CopyFrom(CandidatesKeyPairs[0].PublicKey)
            }.ToBytesValue())).ToConsensusHeaderInformation().Round;

            await oneCandidate.NextRound.SendAsync(thirdRound);

            var cheatInformation = (await oneCandidate.GetInformationToUpdateConsensus.CallAsync(
                new AElfConsensusTriggerInformation
                {
                    Behaviour = AElfConsensusBehaviour.UpdateValue,
                    PreviousRandomHash = Hash.FromMessage(randomHash), // Not same as before.
                    RandomHash = Hash.FromString("hash6"), // Don't care this value in current test case.
                    Pubkey = ByteString.CopyFrom(CandidatesKeyPairs[0].PublicKey)
                }.ToBytesValue())).ToConsensusHeaderInformation();
            await oneCandidate.UpdateValue.SendAsync(
                cheatInformation.Round.ExtractInformationToUpdateConsensus(CandidatesKeyPairs[0].PublicKey.ToHex()));
        }
    }
}