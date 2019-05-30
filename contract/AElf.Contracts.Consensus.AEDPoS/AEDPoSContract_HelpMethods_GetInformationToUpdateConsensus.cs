using AElf.Sdk.CSharp;
using AElf.Types;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Contracts.Consensus.AEDPoS
{
    public partial class AEDPoSContract
    {
        private AElfConsensusHeaderInformation GetInformationToUpdateConsensusToPublishOutValue(Round currentRound,
            string publicKey, AElfConsensusTriggerInformation triggerInformation)
        {
            currentRound.RealTimeMinersInformation[publicKey].ProducedTinyBlocks = currentRound
                .RealTimeMinersInformation[publicKey].ProducedTinyBlocks.Add(1);
            currentRound.RealTimeMinersInformation[publicKey].ProducedBlocks =
                currentRound.RealTimeMinersInformation[publicKey].ProducedBlocks.Add(1);
            currentRound.RealTimeMinersInformation[publicKey].ActualMiningTimes
                .Add(Context.CurrentBlockTime.ToTimestamp());

            Assert(triggerInformation.RandomHash != null, "Random hash should not be null.");

            var inValue = currentRound.CalculateInValue(triggerInformation.RandomHash);
            var outValue = Hash.FromMessage(inValue);
            var signature =
                Hash.FromTwoHashes(outValue, triggerInformation.RandomHash); // Just initial signature value.
            var previousInValue = Hash.Empty; // Just initial previous in value.

            if (TryToGetPreviousRoundInformation(out var previousRound) && !IsJustChangedTerm(out _))
            {
                signature = previousRound.CalculateSignature(inValue);
                if (triggerInformation.PreviousRandomHash != Hash.Empty)
                {
                    // If PreviousRandomHash is Hash.Empty, it means the sender unable or unwilling to publish his previous in value.
                    previousInValue = previousRound.CalculateInValue(triggerInformation.PreviousRandomHash);
                }
            }

            var updatedRound = currentRound.ApplyNormalConsensusData(publicKey, previousInValue,
                outValue, signature);

            ShareAndRecoverInValue(updatedRound, previousRound, inValue, publicKey);

            // To publish Out Value.
            return new AElfConsensusHeaderInformation
            {
                SenderPublicKey = publicKey.ToByteString(),
                Round = updatedRound,
                Behaviour = triggerInformation.Behaviour
            };
        }

        private AElfConsensusHeaderInformation GetInformationToUpdateConsensusForTinyBlock(Round currentRound,
            string publicKey, AElfConsensusTriggerInformation triggerInformation)
        {
            currentRound.RealTimeMinersInformation[publicKey].ProducedTinyBlocks = currentRound
                .RealTimeMinersInformation[publicKey].ProducedTinyBlocks.Add(1);
            currentRound.RealTimeMinersInformation[publicKey].ProducedBlocks =
                currentRound.RealTimeMinersInformation[publicKey].ProducedBlocks.Add(1);
            currentRound.RealTimeMinersInformation[publicKey].ActualMiningTimes
                .Add(Context.CurrentBlockTime.ToTimestamp());

            return new AElfConsensusHeaderInformation
            {
                SenderPublicKey = publicKey.ToByteString(),
                Round = currentRound,
                Behaviour = triggerInformation.Behaviour
            };
        }

        private AElfConsensusHeaderInformation GetInformationToUpdateConsensusForNextRound(Round currentRound,
            string publicKey, AElfConsensusTriggerInformation triggerInformation)
        {
            Assert(
                GenerateNextRoundInformation(currentRound, Context.CurrentBlockTime, out var nextRound),
                "Failed to generate next round information.");
            nextRound.RealTimeMinersInformation[publicKey].ProducedBlocks =
                nextRound.RealTimeMinersInformation[publicKey].ProducedBlocks.Add(1);
            Context.LogDebug(() => $"Mined blocks: {nextRound.GetMinedBlocks()}");
            nextRound.ExtraBlockProducerOfPreviousRound = publicKey;

            nextRound.RealTimeMinersInformation[publicKey].ProducedTinyBlocks = 1;
            nextRound.RealTimeMinersInformation[publicKey].ActualMiningTimes
                .Add(Context.CurrentBlockTime.ToTimestamp());

            return new AElfConsensusHeaderInformation
            {
                SenderPublicKey = publicKey.ToByteString(),
                Round = nextRound,
                Behaviour = triggerInformation.Behaviour
            };
        }

        private AElfConsensusHeaderInformation GetInformationToUpdateConsensusForNextTerm(string publicKey,
            AElfConsensusTriggerInformation triggerInformation)
        {
            Assert(TryToGetMiningInterval(out var miningInterval), "Failed to get mining interval.");
            var firstRoundOfNextTerm = GenerateFirstRoundOfNextTerm(publicKey, miningInterval);
            Assert(firstRoundOfNextTerm.RoundId != 0, "Failed to generate new round information.");
            return new AElfConsensusHeaderInformation
            {
                SenderPublicKey = publicKey.ToByteString(),
                Round = firstRoundOfNextTerm,
                Behaviour = triggerInformation.Behaviour
            };
        }
    }
}