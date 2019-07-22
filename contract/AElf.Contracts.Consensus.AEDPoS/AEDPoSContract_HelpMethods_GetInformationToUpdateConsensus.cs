using System.Linq;
using AElf.Sdk.CSharp;
using AElf.Types;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Contracts.Consensus.AEDPoS
{
    public partial class AEDPoSContract
    {
        private BytesValue GetConsensusBlockExtraData(BytesValue input, bool withSecretSharingInformation = false)
        {
            var triggerInformation = new AElfConsensusTriggerInformation();
            triggerInformation.MergeFrom(input.Value);

            Assert(triggerInformation.Pubkey.Any(), "Invalid public key.");

            if (!TryToGetCurrentRoundInformation(out var currentRound))
            {
                Assert(false, "Failed to get current round information.");
            }

            var publicKeyBytes = triggerInformation.Pubkey;
            var publicKey = publicKeyBytes.ToHex();

            LogIfPreviousMinerHasNotProduceEnoughTinyBlocks(currentRound, publicKey);

            var information = new AElfConsensusHeaderInformation();
            switch (triggerInformation.Behaviour)
            {
                case AElfConsensusBehaviour.UpdateValueWithoutPreviousInValue:
                case AElfConsensusBehaviour.UpdateValue:
                    information = GetInformationToUpdateConsensusToPublishOutValue(currentRound, publicKey,
                        triggerInformation);
                    break;
                case AElfConsensusBehaviour.TinyBlock:
                    information = GetInformationToUpdateConsensusForTinyBlock(currentRound, publicKey,
                        triggerInformation);
                    break;

                case AElfConsensusBehaviour.NextRound:
                    information = GetInformationToUpdateConsensusForNextRound(currentRound, publicKey,
                        triggerInformation);
                    break;

                case AElfConsensusBehaviour.NextTerm:
                    information = GetInformationToUpdateConsensusForNextTerm(publicKey, triggerInformation);
                    break;
            }

            if (!withSecretSharingInformation)
            {
                information.Round.DeleteSecretSharingInformation();
            }

            return information.ToBytesValue();
        }

        private AElfConsensusHeaderInformation GetInformationToUpdateConsensusToPublishOutValue(Round currentRound,
            string publicKey, AElfConsensusTriggerInformation triggerInformation)
        {
            currentRound.RealTimeMinersInformation[publicKey].ProducedTinyBlocks = currentRound
                .RealTimeMinersInformation[publicKey].ProducedTinyBlocks.Add(1);
            currentRound.RealTimeMinersInformation[publicKey].ProducedBlocks =
                currentRound.RealTimeMinersInformation[publicKey].ProducedBlocks.Add(1);
            currentRound.RealTimeMinersInformation[publicKey].ActualMiningTimes
                .Add(Context.CurrentBlockTime);

            Assert(triggerInformation.RandomHash != null, "Random hash should not be null.");

            var inValue = currentRound.CalculateInValue(triggerInformation.RandomHash);
            var outValue = Hash.FromMessage(inValue);
            var signature =
                Hash.FromTwoHashes(outValue, triggerInformation.RandomHash); // Just initial signature value.
            var previousInValue = Hash.Empty; // Just initial previous in value.

            if (TryToGetPreviousRoundInformation(out var previousRound) && !IsFirstRoundOfCurrentTerm(out _))
            {
                signature = previousRound.CalculateSignature(inValue);
                if (triggerInformation.PreviousRandomHash != Hash.Empty)
                {
                    // If PreviousRandomHash is Hash.Empty, it means the sender unable or unwilling to publish his previous in value.
                    previousInValue = previousRound.CalculateInValue(triggerInformation.PreviousRandomHash);
                    // Self check.
                    if (Hash.FromMessage(previousInValue) !=
                        previousRound.RealTimeMinersInformation[publicKey].OutValue)
                    {
                        Context.LogDebug(() => "Failed to produce block at previous round?");
                        previousInValue = Hash.Empty;
                    }
                }
            }

            var updatedRound = currentRound.ApplyNormalConsensusData(publicKey, previousInValue,
                outValue, signature);

            updatedRound.RealTimeMinersInformation[publicKey].ImpliedIrreversibleBlockHeight = Context.CurrentHeight;

            ShareInValueOfCurrentRound(updatedRound, previousRound, inValue, publicKey);

            // To publish Out Value.
            return new AElfConsensusHeaderInformation
            {
                SenderPubkey = publicKey.ToByteString(),
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
                .Add(Context.CurrentBlockTime);

            return new AElfConsensusHeaderInformation
            {
                SenderPubkey = publicKey.ToByteString(),
                Round = currentRound,
                Behaviour = triggerInformation.Behaviour
            };
        }

        private AElfConsensusHeaderInformation GetInformationToUpdateConsensusForNextRound(Round currentRound,
            string publicKey, AElfConsensusTriggerInformation triggerInformation)
        {
            if (!GenerateNextRoundInformation(currentRound, Context.CurrentBlockTime, out var nextRound))
            {
                Assert(false, "Failed to generate next round information.");
            }

            if (!nextRound.RealTimeMinersInformation.Keys.Contains(publicKey))
            {
                return new AElfConsensusHeaderInformation
                {
                    SenderPubkey = publicKey.ToByteString(),
                    Round = nextRound,
                    Behaviour = triggerInformation.Behaviour
                };
            }

            RevealSharedInValues(currentRound, publicKey);
            
            nextRound.RealTimeMinersInformation[publicKey].ProducedBlocks =
                nextRound.RealTimeMinersInformation[publicKey].ProducedBlocks.Add(1);
            Context.LogDebug(() => $"Mined blocks: {nextRound.GetMinedBlocks()}");
            nextRound.ExtraBlockProducerOfPreviousRound = publicKey;

            nextRound.RealTimeMinersInformation[publicKey].ProducedTinyBlocks = 1;
            nextRound.RealTimeMinersInformation[publicKey].ActualMiningTimes
                .Add(Context.CurrentBlockTime);

            return new AElfConsensusHeaderInformation
            {
                SenderPubkey = publicKey.ToByteString(),
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
                SenderPubkey = publicKey.ToByteString(),
                Round = firstRoundOfNextTerm,
                Behaviour = triggerInformation.Behaviour
            };
        }
    }
}