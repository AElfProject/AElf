using System;
using System.Linq;
using System.Threading.Tasks;
using AElf.Contracts.Economic.TestBase;
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
            //await InitializeVoters();
            await InitializeCandidates(AEDPoSContractTestConstants.InitialMinersCount);

            var firstRound = await AEDPoSContractStub.GetCurrentRoundInformation.CallAsync(new Empty());

            var randomHashes = Enumerable.Range(0, AEDPoSContractTestConstants.InitialMinersCount)
                .Select(_ => Hash.FromString("hash")).ToList();
            var triggers = Enumerable.Range(0, AEDPoSContractTestConstants.InitialMinersCount).Select(i =>
                new AElfConsensusTriggerInformation
                {
                    Pubkey = ByteString.CopyFrom(InitialCoreDataCenterKeyPairs[i].PublicKey),
                    RandomHash = randomHashes[i]
                }).ToDictionary(t => t.Pubkey.ToHex(), t => t);

            var voter = GetElectionContractTester(VoterKeyPairs[0]);
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
            foreach (var candidateKeyPair in ValidationDataCenterCandidateKeyPairs
                .Take(AEDPoSContractTestConstants.InitialMinersCount).Append(oneMoreCandidateKeyPair))
            {
                await voter.Vote.SendAsync(new VoteMinerInput
                {
                    CandidatePubkey = candidateKeyPair.PublicKey.ToHex(),
                    Amount = 10000 - new Random().Next(1, 200) * count,
                    EndTimestamp = TimestampHelper.GetUtcNow().AddDays(100)
                });
                count++;
            }

            foreach (var minerInRound in firstRound.RealTimeMinersInformation.Values.OrderBy(m => m.Order))
            {
                var currentKeyPair =
                    InitialCoreDataCenterKeyPairs.First(p => p.PublicKey.ToHex() == minerInRound.Pubkey);

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

            var changeTermTime = BlockchainStartTimestamp.ToDateTime()
                .AddMinutes(AEDPoSContractTestConstants.TimeEachTerm + 1);
            BlockTimeProvider.SetBlockTime(changeTermTime.ToTimestamp());

            var nextTermInformationBytes = await AEDPoSContractStub.GetInformationToUpdateConsensus.CallAsync(
                new AElfConsensusTriggerInformation
                {
                    Behaviour = AElfConsensusBehaviour.NextTerm,
                    Pubkey = ByteString.CopyFrom(BootMinerKeyPair.PublicKey)
                }.ToBytesValue());
            var nextTermInformation = new AElfConsensusHeaderInformation();
            nextTermInformation.MergeFrom(nextTermInformationBytes.Value);
            await AEDPoSContractStub.NextTerm.SendAsync(nextTermInformation.Round);

            // First candidate cheat others with in value.
            var oneCandidate = GetAEDPoSContractStub(ValidationDataCenterKeyPairs[0]);
            var anotherCandidate = GetAEDPoSContractStub(ValidationDataCenterKeyPairs[1]);
            var randomHash = Hash.FromString("hash2");
            var input = new AElfConsensusTriggerInformation
            {
                Behaviour = AElfConsensusBehaviour.UpdateValue,
                PreviousRandomHash = Hash.Empty,
                RandomHash = randomHash,
                Pubkey = ByteString.CopyFrom(ValidationDataCenterKeyPairs[0].PublicKey)
            };
            var informationOfSecondRoundBytes =
                await AEDPoSContractStub.GetInformationToUpdateConsensus.CallAsync(input.ToBytesValue());
            var informationOfSecondRound = new AElfConsensusHeaderInformation();
            informationOfSecondRound.MergeFrom(informationOfSecondRoundBytes.Value);
            if (informationOfSecondRound.Round == null)
                _testOutputHelper.WriteLine(informationOfSecondRound.ToString());
            await oneCandidate.UpdateValue.SendAsync(
                informationOfSecondRound.Round.ExtractInformationToUpdateConsensus(ValidationDataCenterKeyPairs[0]
                    .PublicKey
                    .ToHex()));

            var thirdRoundStartTime = changeTermTime.AddMinutes(AEDPoSContractTestConstants.TimeEachTerm + 2);
            BlockTimeProvider.SetBlockTime(thirdRoundStartTime.ToTimestamp());

            var informationOfThirdRoundBytes = await AEDPoSContractStub.GetInformationToUpdateConsensus.CallAsync(
                new AElfConsensusTriggerInformation
                {
                    Behaviour = AElfConsensusBehaviour.NextRound,
                    Pubkey = ByteString.CopyFrom(ValidationDataCenterKeyPairs[0].PublicKey)
                }.ToBytesValue());
            var informationOfThirdRound = new AElfConsensusHeaderInformation();
            informationOfThirdRound.MergeFrom(informationOfThirdRoundBytes.Value);
            var thirdRound = informationOfThirdRound.Round;
            await oneCandidate.NextRound.SendAsync(thirdRound);

            var cheatInformationBytes = await AEDPoSContractStub.GetInformationToUpdateConsensus.CallAsync(
                new AElfConsensusTriggerInformation
                {
                    Behaviour = AElfConsensusBehaviour.UpdateValue,
                    PreviousRandomHash = Hash.FromMessage(randomHash), // Not same as before.
                    RandomHash = Hash.FromString("hash3"), // Don't care this value in current test case.
                    Pubkey = ByteString.CopyFrom(ValidationDataCenterKeyPairs[0].PublicKey)
                }.ToBytesValue());
            var cheatInformation = new AElfConsensusHeaderInformation();
            cheatInformation.MergeFrom(cheatInformationBytes.Value);
            await oneCandidate.UpdateValue.SendAsync(
                cheatInformation.Round.ExtractInformationToUpdateConsensus(ValidationDataCenterKeyPairs[0].PublicKey
                    .ToHex()));

            // The other miner generate information of next round.
            var fourthRoundStartTime = changeTermTime.AddMinutes(AEDPoSContractTestConstants.TimeEachTerm + 3);
            BlockTimeProvider.SetBlockTime(fourthRoundStartTime.ToTimestamp());
            var informationOfFourthRoundBytes = await AEDPoSContractStub.GetInformationToUpdateConsensus.CallAsync(
                new AElfConsensusTriggerInformation
                {
                    Behaviour = AElfConsensusBehaviour.NextRound,
                    Pubkey = ByteString.CopyFrom(ValidationDataCenterKeyPairs[1].PublicKey)
                }.ToBytesValue());
            var informationOfFourthRound = new AElfConsensusHeaderInformation();
            informationOfFourthRound.MergeFrom(informationOfFourthRoundBytes.Value);
            var fourthRound = informationOfFourthRound.Round;

            fourthRound.RealTimeMinersInformation.Keys.ShouldNotContain(ValidationDataCenterKeyPairs[0].PublicKey
                .ToHex());
            fourthRound.RealTimeMinersInformation.Keys.ShouldContain(oneMoreCandidateKeyPair.PublicKey.ToHex());
        }

        [Fact]
        public async Task Candidates_NotEnough_Test()
        {
            await ElectionContractStub.RegisterElectionVotingEvent.SendAsync(new Empty());

            //await InitializeVoters();
            await InitializeCandidates(EconomicContractsTestConstants.InitialCoreDataCenterCount);

            var firstRound = await AEDPoSContractStub.GetCurrentRoundInformation.CallAsync(new Empty());

            var randomHashes = Enumerable.Range(0, EconomicContractsTestConstants.InitialCoreDataCenterCount)
                .Select(_ => Hash.FromString("hash3")).ToList();
            var triggers = Enumerable.Range(0, EconomicContractsTestConstants.InitialCoreDataCenterCount).Select(i =>
                new AElfConsensusTriggerInformation
                {
                    Pubkey = ByteString.CopyFrom(InitialCoreDataCenterKeyPairs[i].PublicKey),
                    RandomHash = randomHashes[i]
                }).ToDictionary(t => t.Pubkey.ToHex(), t => t);

            var voter = GetElectionContractTester(VoterKeyPairs[0]);

            foreach (var candidateKeyPair in ValidationDataCenterKeyPairs.Take(EconomicContractsTestConstants
                .InitialCoreDataCenterCount))
            {
                await voter.Vote.SendAsync(new VoteMinerInput
                {
                    CandidatePubkey = candidateKeyPair.PublicKey.ToHex(),
                    Amount = 100 + new Random().Next(1, 200),
                    EndTimestamp = TimestampHelper.GetUtcNow().AddDays(100)
                });
            }

            foreach (var minerInRound in firstRound.RealTimeMinersInformation.Values.OrderBy(m => m.Order))
            {
                var currentKeyPair =
                    InitialCoreDataCenterKeyPairs.First(p => p.PublicKey.ToHex() == minerInRound.Pubkey);

                KeyPairProvider.SetKeyPair(currentKeyPair);

                BlockTimeProvider.SetBlockTime(minerInRound.ExpectedMiningTime);

                var tester = GetAEDPoSContractStub(currentKeyPair);
                var headerInformation =
                    (await AEDPoSContractStub.GetInformationToUpdateConsensus.CallAsync(triggers[minerInRound.Pubkey]
                        .ToBytesValue())).ToConsensusHeaderInformation();

                // Update consensus information.
                var toUpdate = headerInformation.Round.ExtractInformationToUpdateConsensus(minerInRound.Pubkey);
                await tester.UpdateValue.SendAsync(toUpdate);
            }

            var changeTermTime = BlockchainStartTimestamp.ToDateTime()
                .AddMinutes(AEDPoSContractTestConstants.TimeEachTerm + 1);
            BlockTimeProvider.SetBlockTime(changeTermTime.ToTimestamp());

            var nextTermInformation = (await AEDPoSContractStub.GetInformationToUpdateConsensus.CallAsync(
                new AElfConsensusTriggerInformation
                {
                    Behaviour = AElfConsensusBehaviour.NextTerm,
                    Pubkey = ByteString.CopyFrom(BootMinerKeyPair.PublicKey)
                }.ToBytesValue())).ToConsensusHeaderInformation();

            await AEDPoSContractStub.NextTerm.SendAsync(nextTermInformation.Round);

            // First candidate cheat others with in value.
            var oneCandidate = GetAEDPoSContractStub(ValidationDataCenterKeyPairs[0]);
            var anotherCandidate = GetAEDPoSContractStub(ValidationDataCenterKeyPairs[1]);
            var randomHash = Hash.FromString("hash5");
            var informationOfSecondRound = (await AEDPoSContractStub.GetInformationToUpdateConsensus.CallAsync(
                new AElfConsensusTriggerInformation
                {
                    Behaviour = AElfConsensusBehaviour.UpdateValue,
                    PreviousRandomHash = Hash.Empty,
                    RandomHash = randomHash,
                    Pubkey = ByteString.CopyFrom(ValidationDataCenterKeyPairs[0].PublicKey)
                }.ToBytesValue())).ToConsensusHeaderInformation();
            var updateResult = await oneCandidate.UpdateValue.SendAsync(
                informationOfSecondRound.Round.ExtractInformationToUpdateConsensus(ValidationDataCenterKeyPairs[0]
                    .PublicKey.ToHex()));

            var thirdRoundStartTime = changeTermTime.AddMinutes(AEDPoSContractTestConstants.TimeEachTerm + 2);
            BlockTimeProvider.SetBlockTime(thirdRoundStartTime.ToTimestamp());
            var thirdRound = (await AEDPoSContractStub.GetInformationToUpdateConsensus.CallAsync(
                new AElfConsensusTriggerInformation
                {
                    Behaviour = AElfConsensusBehaviour.NextRound,
                    Pubkey = ByteString.CopyFrom(ValidationDataCenterKeyPairs[0].PublicKey)
                }.ToBytesValue())).ToConsensusHeaderInformation().Round;

            await oneCandidate.NextRound.SendAsync(thirdRound);

            var cheatInformation = (await AEDPoSContractStub.GetInformationToUpdateConsensus.CallAsync(
                new AElfConsensusTriggerInformation
                {
                    Behaviour = AElfConsensusBehaviour.UpdateValue,
                    PreviousRandomHash = Hash.FromMessage(randomHash), // Not same as before.
                    RandomHash = Hash.FromString("RandomHash"), // Don't care this value in current test case.
                    Pubkey = ByteString.CopyFrom(ValidationDataCenterKeyPairs[0].PublicKey)
                }.ToBytesValue())).ToConsensusHeaderInformation();
            await oneCandidate.UpdateValue.SendAsync(
                cheatInformation.Round.ExtractInformationToUpdateConsensus(ValidationDataCenterKeyPairs[0].PublicKey
                    .ToHex()));
        }

        [Fact]
        public async Task Update_TinyBlockInformation_Test()
        {
            await AEDPoSContract_FirstRound_BootMiner_Test();

            var roundInfo = await AEDPoSContractStub.GetCurrentRoundInformation.CallAsync(new Empty());

            BlockTimeProvider.SetBlockTime(BlockchainStartTimestamp + new Duration
            {
                Seconds = AEDPoSContractTestConstants.MiningInterval.Div(1000)
            });
            var input = new TinyBlockInput
            {
                RoundId = roundInfo.RoundId,
                ProducedBlocks = 4,
                ActualMiningTime = BlockTimeProvider.GetBlockTime()
            };
            var transactionResult = await AEDPoSContractStub.UpdateTinyBlockInformation.SendAsync(input);
            transactionResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
        }

        [Fact]
        public async Task Update_ConsensusInformation_Test()
        {
            await AEDPoSContract_FirstRound_BootMiner_Test();

            //just verify method, will add other logic
            {
                var input = new ConsensusInformation
                {
                    Value = ByteString.CopyFromUtf8("test")
                };
                var transactionResult = await AEDPoSContractStub.UpdateConsensusInformation.SendAsync(input);
                transactionResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
            }
        }
    }
}