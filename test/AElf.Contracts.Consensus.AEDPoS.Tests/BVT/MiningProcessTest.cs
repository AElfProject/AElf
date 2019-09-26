using System;
using System.Linq;
using System.Threading.Tasks;
using AElf.Contracts.Economic.TestBase;
using AElf.Contracts.Election;
using AElf.Contracts.MultiToken;
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
        [Fact]
        public async Task Candidates_NotEnough_Test()
        {
            //await ElectionContractStub.RegisterElectionVotingEvent.SendAsync(new Empty());

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
                    (await AEDPoSContractStub.GetConsensusExtraData.CallAsync(triggers[minerInRound.Pubkey]
                        .ToBytesValue())).ToConsensusHeaderInformation();

                // Update consensus information.
                var toUpdate = headerInformation.Round.ExtractInformationToUpdateConsensus(minerInRound.Pubkey);
                await tester.UpdateValue.SendAsync(toUpdate);
            }

            var changeTermTime = BlockchainStartTimestamp.ToDateTime()
                .AddMinutes(AEDPoSContractTestConstants.TimeEachTerm + 1);
            BlockTimeProvider.SetBlockTime(changeTermTime.ToTimestamp());

            var nextTermInformation = (await AEDPoSContractStub.GetConsensusExtraData.CallAsync(
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
            var informationOfSecondRound = (await AEDPoSContractStub.GetConsensusExtraData.CallAsync(
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
            var thirdRound = (await AEDPoSContractStub.GetConsensusExtraData.CallAsync(
                new AElfConsensusTriggerInformation
                {
                    Behaviour = AElfConsensusBehaviour.NextRound,
                    Pubkey = ByteString.CopyFrom(ValidationDataCenterKeyPairs[0].PublicKey)
                }.ToBytesValue())).ToConsensusHeaderInformation().Round;

            await oneCandidate.NextRound.SendAsync(thirdRound);

            var cheatInformation = (await AEDPoSContractStub.GetConsensusExtraData.CallAsync(
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