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

        /// <summary>
        /// test:Change the number of miners when term changed
        /// </summary>
        /// <returns></returns>
        [Fact]
        public async Task TermTest()
        {
            var maxCount = CandidatesKeyPairs.Count;
            await InitializeVoters();
            await InitializeCandidates(maxCount);

            var firstRound = await BootMiner.GetCurrentRoundInformation.CallAsync(new Empty());

            var randomHashes = Enumerable.Range(0, AEDPoSContractTestConstants.InitialMinersCount).Select(_ => Hash.Generate()).ToList();
            var triggers = Enumerable.Range(0, AEDPoSContractTestConstants.InitialMinersCount).Select(i => new AElfConsensusTriggerInformation
            {
                PublicKey = ByteString.CopyFrom(InitialMinersKeyPairs[i].PublicKey),
                RandomHash = randomHashes[i]
            }).ToDictionary(t => t.PublicKey.ToHex(), t => t);

            var voter = GetElectionContractTester(VotersKeyPairs[0]);

            foreach (var candidateKeyPair in CandidatesKeyPairs)
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
            }

            var changeTermTime = BlockchainStartTimestamp.ToDateTime();
            BlockTimeProvider.SetBlockTime(changeTermTime);

            var nextTermInformation = (await BootMiner.GetInformationToUpdateConsensus.CallAsync(
                new AElfConsensusTriggerInformation
                {
                    Behaviour = AElfConsensusBehaviour.NextRound,
                    PublicKey = ByteString.CopyFrom(BootMinerKeyPair.PublicKey)
                }.ToBytesValue())).ToConsensusHeaderInformation();

            await BootMiner.NextRound.SendAsync(nextTermInformation.Round);
            changeTermTime = BlockchainStartTimestamp.ToDateTime().AddMinutes(2).AddSeconds(10);
            BlockTimeProvider.SetBlockTime(changeTermTime);

            nextTermInformation = (await BootMiner.GetInformationToUpdateConsensus.CallAsync(
                new AElfConsensusTriggerInformation
                {
                    Behaviour = AElfConsensusBehaviour.NextTerm,
                    PublicKey = ByteString.CopyFrom(BootMinerKeyPair.PublicKey)
                }.ToBytesValue())).ToConsensusHeaderInformation();

            await BootMiner.NextTerm.SendAsync(nextTermInformation.Round);
            
            var termCount = 0;
            var minerCount = 0;
            while (minerCount < maxCount)
            {
                var currentRound = await BootMiner.GetCurrentRoundInformation.CallAsync(new Empty());

                minerCount = currentRound.RealTimeMinersInformation.Count;
                Assert.Equal(termCount+3,currentRound.RoundNumber);
                Assert.Equal( 7.Add(termCount.Mul(2)) ,minerCount);
                
                
                changeTermTime = BlockchainStartTimestamp.ToDateTime()
                    .AddMinutes((termCount+2).Mul(2)).AddSeconds(10);
                BlockTimeProvider.SetBlockTime(changeTermTime);
                var nextRoundInformation = (await BootMiner.GetInformationToUpdateConsensus.CallAsync(
                    new AElfConsensusTriggerInformation
                    {
                        Behaviour = AElfConsensusBehaviour.NextTerm,
                        PublicKey = currentRound.RealTimeMinersInformation.ElementAt(0).Value.PublicKey.ToByteString()
                    }.ToBytesValue())).ToConsensusHeaderInformation();

                await BootMiner.NextTerm.SendAsync(nextRoundInformation.Round);
                termCount++;

            }
            
        }
        
    }
}