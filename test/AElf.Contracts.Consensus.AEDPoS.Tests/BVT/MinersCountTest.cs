using System;
using System.Linq;
using System.Threading.Tasks;
using AElf.Contracts.Economic.TestBase;
using AElf.Contracts.Election;
using AElf.Kernel;
using AElf.Sdk.CSharp;
using AElf.Types;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Shouldly;
using Volo.Abp.Threading;
using Xunit;

namespace AElf.Contracts.Consensus.AEDPoS
{
    public partial class AEDPoSTest
    {
        /// <summary>
        /// test:Change the number of miners when term changed
        /// </summary>
        /// <returns></returns>
        [Fact]
        public async Task AEDPoSContract_ChangeMinersCount_Test()
        {
            const int termIntervalMin = 31536000 / 60;
            
            await ElectionContractStub.RegisterElectionVotingEvent.SendAsync(new Empty());

            var maxCount = ValidationDataCenterKeyPairs.Count;
            await InitializeCandidates(maxCount);

            var firstRound = await AEDPoSContractStub.GetCurrentRoundInformation.CallAsync(new Empty());

            var randomHashes = Enumerable.Range(0, EconomicContractsTestConstants.InitialCoreDataCenterCount)
                .Select(_ => Hash.FromString("randomHashes")).ToList();
            var triggers = Enumerable.Range(0, EconomicContractsTestConstants.InitialCoreDataCenterCount).Select(i =>
                new AElfConsensusTriggerInformation
                {
                    Pubkey = ByteString.CopyFrom(InitialCoreDataCenterKeyPairs[i].PublicKey),
                    RandomHash = randomHashes[i]
                }).ToDictionary(t => t.Pubkey.ToHex(), t => t);

            var voter = GetElectionContractTester(VoterKeyPairs[0]);
            foreach (var candidateKeyPair in ValidationDataCenterKeyPairs)
            {
                var voteResult = await voter.Vote.SendAsync(new VoteMinerInput
                {
                    CandidatePubkey = candidateKeyPair.PublicKey.ToHex(),
                    Amount = 10 + new Random().Next(1, 10),
                    EndTimestamp = TimestampHelper.GetUtcNow().AddDays(100)
                });
                voteResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
            }

            foreach (var minerInRound in firstRound.RealTimeMinersInformation.Values.OrderBy(m => m.Order))
            {
                var currentKeyPair = InitialCoreDataCenterKeyPairs.First(p => p.PublicKey.ToHex() == minerInRound.Pubkey);

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

            var changeTermTime = BlockchainStartTimestamp.ToDateTime();
            BlockTimeProvider.SetBlockTime(changeTermTime.ToTimestamp());

            var nextTermInformation = (await AEDPoSContractStub.GetInformationToUpdateConsensus.CallAsync(
                new AElfConsensusTriggerInformation
                {
                    Behaviour = AElfConsensusBehaviour.NextRound,
                    Pubkey = ByteString.CopyFrom(BootMinerKeyPair.PublicKey)
                }.ToBytesValue())).ToConsensusHeaderInformation();

            await AEDPoSContractStub.NextRound.SendAsync(nextTermInformation.Round);
            changeTermTime = BlockchainStartTimestamp.ToDateTime().AddMinutes(termIntervalMin).AddSeconds(10);
            BlockTimeProvider.SetBlockTime(changeTermTime.ToTimestamp());

            nextTermInformation = (await AEDPoSContractStub.GetInformationToUpdateConsensus.CallAsync(
                new AElfConsensusTriggerInformation
                {
                    Behaviour = AElfConsensusBehaviour.NextTerm,
                    Pubkey = ByteString.CopyFrom(BootMinerKeyPair.PublicKey)
                }.ToBytesValue())).ToConsensusHeaderInformation();

            var transactionResult = await AEDPoSContractStub.NextTerm.SendAsync(nextTermInformation.Round);
            transactionResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);

            var termCount = 0;
            var minerCount = 0;
            while (minerCount < maxCount)
            {
                var currentRound = await AEDPoSContractStub.GetCurrentRoundInformation.CallAsync(new Empty());

                minerCount = currentRound.RealTimeMinersInformation.Count;
                Assert.Equal(9.Add(termCount.Mul(2)), minerCount);

                changeTermTime = BlockchainStartTimestamp.ToDateTime()
                    .AddMinutes((termCount + 2).Mul(termIntervalMin)).AddSeconds(10);
                BlockTimeProvider.SetBlockTime(changeTermTime.ToTimestamp());
                var nextRoundInformation = (await AEDPoSContractStub.GetInformationToUpdateConsensus.CallAsync(
                    new AElfConsensusTriggerInformation
                    {
                        Behaviour = AElfConsensusBehaviour.NextTerm,
                        Pubkey = currentRound.RealTimeMinersInformation.ElementAt(0).Value.Pubkey.ToByteString()
                    }.ToBytesValue())).ToConsensusHeaderInformation();

                await AEDPoSContractStub.NextTerm.SendAsync(nextRoundInformation.Round);
                termCount++;
            }
        }

        [Fact]
        public async Task AEDPoSContract_SetMaximumMinersCount_NoPermission()
        {
            var transactionResult =
                (await AEDPoSContractStub.SetMaximumMinersCount.SendAsync(new SInt32Value {Value = 100}))
                .TransactionResult;
            transactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
            transactionResult.Error.ShouldContain("No permission");
        }

        [Fact]
        public async Task AEDPoSContract_SetMaximumMinersCount()
        {
            
        }
    }
}