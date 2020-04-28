using System;
using System.Linq;
using System.Threading.Tasks;
using AElf.Contracts.Economic.TestBase;
using AElf.Contracts.Election;
using AElf.CSharp.Core;
using AElf.CSharp.Core.Extension;
using AElf.Kernel;
using AElf.Sdk.CSharp;
using AElf.TestBase;
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
        [IgnoreOnCIFact]
        public async Task AEDPoSContract_ChangeMinersCount_Test()
        {
            const int termIntervalMin = 31536000 / 60;
            
            var maxCount = ValidationDataCenterKeyPairs.Count;
            await InitializeCandidates(maxCount);

            var firstRound = await AEDPoSContractStub.GetCurrentRoundInformation.CallAsync(new Empty());

            var randomHashes = Enumerable.Range(0, EconomicContractsTestConstants.InitialCoreDataCenterCount)
                .Select(_ => HashHelper.ComputeFrom("randomHashes")).ToList();
            var triggers = Enumerable.Range(0, EconomicContractsTestConstants.InitialCoreDataCenterCount).Select(i =>
                new AElfConsensusTriggerInformation
                {
                    Pubkey = ByteString.CopyFrom(InitialCoreDataCenterKeyPairs[i].PublicKey),
                    InValue = randomHashes[i]
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
                    (await AEDPoSContractStub.GetConsensusExtraData.CallAsync(triggers[minerInRound.Pubkey]
                        .ToBytesValue())).ToConsensusHeaderInformation();

                // Update consensus information.
                var toUpdate = headerInformation.Round.ExtractInformationToUpdateConsensus(minerInRound.Pubkey);
                await tester.UpdateValue.SendAsync(toUpdate);
            }

            var changeTermTime = BlockchainStartTimestamp.ToDateTime();
            BlockTimeProvider.SetBlockTime(changeTermTime.ToTimestamp());

            var nextTermInformation = (await AEDPoSContractStub.GetConsensusExtraData.CallAsync(
                new AElfConsensusTriggerInformation
                {
                    Behaviour = AElfConsensusBehaviour.NextRound,
                    Pubkey = ByteString.CopyFrom(BootMinerKeyPair.PublicKey)
                }.ToBytesValue())).ToConsensusHeaderInformation();

            await AEDPoSContractStub.NextRound.SendAsync(nextTermInformation.Round);
            changeTermTime = BlockchainStartTimestamp.ToDateTime().AddMinutes(termIntervalMin).AddSeconds(10);
            BlockTimeProvider.SetBlockTime(changeTermTime.ToTimestamp());

            nextTermInformation = (await AEDPoSContractStub.GetConsensusExtraData.CallAsync(
                new AElfConsensusTriggerInformation
                {
                    Behaviour = AElfConsensusBehaviour.NextTerm,
                    Pubkey = ByteString.CopyFrom(BootMinerKeyPair.PublicKey)
                }.ToBytesValue())).ToConsensusHeaderInformation();

            var transactionResult = await AEDPoSContractStub.NextTerm.SendAsync(nextTermInformation.Round);
            transactionResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);

            var newMinerStub = GetAEDPoSContractStub(ValidationDataCenterKeyPairs[0]);
            var termCount = 0;
            var minerCount = 0;
            while (minerCount < maxCount)
            {
                var currentRound = await newMinerStub.GetCurrentRoundInformation.CallAsync(new Empty());
                var firstPubKey = currentRound.RealTimeMinersInformation.Keys.First();
                newMinerStub = GetAEDPoSContractStub(ValidationDataCenterKeyPairs.First(o =>o.PublicKey.ToHex() == firstPubKey));
                
                minerCount = currentRound.RealTimeMinersInformation.Count;
                Assert.Equal(AEDPoSContractTestConstants.SupposedMinersCount.Add(termCount.Mul(2)), minerCount);

                changeTermTime = BlockchainStartTimestamp.ToDateTime()
                    .AddMinutes((termCount + 2).Mul(termIntervalMin)).AddSeconds(10);
                BlockTimeProvider.SetBlockTime(changeTermTime.ToTimestamp());
                var nextRoundInformation = (await newMinerStub.GetConsensusExtraData.CallAsync(
                    new AElfConsensusTriggerInformation
                    {
                        Behaviour = AElfConsensusBehaviour.NextTerm,
                        Pubkey = currentRound.RealTimeMinersInformation.ElementAt(0).Value.Pubkey.ToByteString()
                    }.ToBytesValue())).ToConsensusHeaderInformation();

                await newMinerStub.NextTerm.SendAsync(nextRoundInformation.Round);
                termCount++;
            }
        }

        [Fact]
        public async Task AEDPoSContract_SetMaximumMinersCount_NoPermission()
        {
            var transactionResult =
                (await AEDPoSContractStub.SetMaximumMinersCount.SendAsync(new Int32Value {Value = 100}))
                .TransactionResult;
            transactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
            transactionResult.Error.ShouldContain("No permission");
        }
    }
}