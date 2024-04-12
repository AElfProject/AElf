using System;
using System.Linq;
using System.Threading.Tasks;
using AElf.Contracts.Economic.TestBase;
using AElf.Contracts.Election;
using AElf.CSharp.Core;
using AElf.CSharp.Core.Extension;
using AElf.Kernel;
using AElf.TestBase;
using AElf.Types;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Shouldly;
using Xunit;

namespace AElf.Contracts.Consensus.AEDPoS;

public partial class AEDPoSTest
{
    /// <summary>
    ///     test:Change the number of miners when term changed
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

        byte[] randomNumber;
        foreach (var minerInRound in firstRound.RealTimeMinersInformation.Values.OrderBy(m => m.Order))
        {
            var currentKeyPair = InitialCoreDataCenterKeyPairs.First(p => p.PublicKey.ToHex() == minerInRound.Pubkey);

            KeyPairProvider.SetKeyPair(currentKeyPair);

            BlockTimeProvider.SetBlockTime(minerInRound.ExpectedMiningTime);

            var tester = GetAEDPoSContractStub(currentKeyPair);
            var headerInformation =
                (await AEDPoSContractStub.GetConsensusExtraData.CallAsync(triggers[minerInRound.Pubkey]
                    .ToBytesValue())).ToConsensusHeaderInformation();

            randomNumber = await GenerateRandomProofAsync(currentKeyPair);
            // Update consensus information.
            var toUpdate =
                headerInformation.Round.ExtractInformationToUpdateConsensus(minerInRound.Pubkey,
                    ByteString.CopyFrom(randomNumber));
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

        var nextRoundInput = NextRoundInput.Parser.ParseFrom(nextTermInformation.Round.ToByteArray());
        randomNumber = await GenerateRandomProofAsync(BootMinerKeyPair);
        nextRoundInput.RandomNumber = ByteString.CopyFrom(randomNumber);
        await AEDPoSContractStub.NextRound.SendAsync(nextRoundInput);
        changeTermTime = BlockchainStartTimestamp.ToDateTime().AddMinutes(termIntervalMin).AddSeconds(10);
        BlockTimeProvider.SetBlockTime(changeTermTime.ToTimestamp());

        nextTermInformation = (await AEDPoSContractStub.GetConsensusExtraData.CallAsync(
            new AElfConsensusTriggerInformation
            {
                Behaviour = AElfConsensusBehaviour.NextTerm,
                Pubkey = ByteString.CopyFrom(BootMinerKeyPair.PublicKey)
            }.ToBytesValue())).ToConsensusHeaderInformation();

        var nextTermInput = NextTermInput.Parser.ParseFrom(nextTermInformation.Round.ToByteArray());
        randomNumber = await GenerateRandomProofAsync(BootMinerKeyPair);
        nextTermInput.RandomNumber = ByteString.CopyFrom(randomNumber);
        var transactionResult = await AEDPoSContractStub.NextTerm.SendAsync(nextTermInput);
        transactionResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);

        var newMinerStub = GetAEDPoSContractStub(ValidationDataCenterKeyPairs[0]);
        var termCount = 0;
        var minerCount = 0;
        while (minerCount < maxCount)
        {
            var currentRound = await newMinerStub.GetCurrentRoundInformation.CallAsync(new Empty());
            var firstPubKey = currentRound.RealTimeMinersInformation.Keys.First();
            var keypair = ValidationDataCenterKeyPairs.First(o => o.PublicKey.ToHex() == firstPubKey);
            newMinerStub = GetAEDPoSContractStub(keypair);

            minerCount = currentRound.RealTimeMinersInformation.Count;
            Assert.Equal(AEDPoSContractTestConstants.SupposedMinersCount.Add(termCount.Mul(2)), minerCount);

            changeTermTime = BlockchainStartTimestamp.ToDateTime()
                .AddMinutes((termCount + 2).Mul(termIntervalMin)).AddSeconds(10);
            BlockTimeProvider.SetBlockTime(changeTermTime.ToTimestamp());
            var nextRoundInformation = (await newMinerStub.GetConsensusExtraData.CallAsync(
                new AElfConsensusTriggerInformation
                {
                    Behaviour = AElfConsensusBehaviour.NextTerm,
                    Pubkey = ByteStringHelper.FromHexString(currentRound.RealTimeMinersInformation.ElementAt(0).Value
                        .Pubkey)
                }.ToBytesValue())).ToConsensusHeaderInformation();
            nextTermInput = NextTermInput.Parser.ParseFrom(nextRoundInformation.Round.ToByteArray());
            randomNumber = await GenerateRandomProofAsync(keypair);
            nextTermInput.RandomNumber = ByteString.CopyFrom(randomNumber);
            await newMinerStub.NextTerm.SendAsync(nextTermInput);
            termCount++;
        }
    }

    [Fact]
    public async Task AEDPoSContract_SetMaximumMinersCount_NoPermission()
    {
        var transactionResult =
            (await AEDPoSContractStub.SetMaximumMinersCount.SendAsync(new Int32Value { Value = 100 }))
            .TransactionResult;
        transactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
        transactionResult.Error.ShouldContain("No permission");
    }
    
    [Fact]
    public async Task AEDPoSContract_SetMinerIncreaseInterval_NoPermission()
    {
        var transactionResult =
            (await AEDPoSContractStub.SetMinerIncreaseInterval.SendAsync(new Int64Value { Value = 100 }))
            .TransactionResult;
        transactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
        transactionResult.Error.ShouldContain("No permission to set miner increase interval.");
    }
}