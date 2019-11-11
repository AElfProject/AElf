using System.Linq;
using AElf.Sdk.CSharp;
using AElf.Types;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Contracts.Consensus.AEDPoS
{
    // ReSharper disable once InconsistentNaming
    public partial class AEDPoSContract
    {
        private BytesValue GetConsensusBlockExtraData(BytesValue input, bool isGeneratingTransactions = false)
        {
            var triggerInformation = new AElfConsensusTriggerInformation();
            triggerInformation.MergeFrom(input.Value);

            Assert(triggerInformation.Pubkey.Any(), "Invalid public key.");

            if (!TryToGetCurrentRoundInformation(out var currentRound))
            {
                Assert(false, "Failed to get current round information.");
            }

            var publicKeyBytes = triggerInformation.Pubkey;
            var pubkey = publicKeyBytes.ToHex();

            LogIfPreviousMinerHasNotProduceEnoughTinyBlocks(currentRound, pubkey);

            var information = new AElfConsensusHeaderInformation();
            switch (triggerInformation.Behaviour)
            {
                case AElfConsensusBehaviour.UpdateValue:
                    information = GetConsensusExtraDataToPublishOutValue(currentRound, pubkey,
                        triggerInformation);
                    if (!isGeneratingTransactions)
                    {
                        information.Round = information.Round.GetUpdateValueRound(pubkey);
                    }

                    break;

                case AElfConsensusBehaviour.TinyBlock:
                    information = GetConsensusExtraDataForTinyBlock(currentRound, pubkey,
                        triggerInformation);
                    break;

                case AElfConsensusBehaviour.NextRound:
                    information = GetConsensusExtraDataForNextRound(currentRound, pubkey,
                        triggerInformation);
                    break;

                case AElfConsensusBehaviour.NextTerm:
                    information = GetConsensusExtraDataForNextTerm(pubkey, triggerInformation);
                    break;
            }

            if (!isGeneratingTransactions)
            {
                information.Round.DeleteSecretSharingInformation();
            }

            return information.ToBytesValue();
        }

        private AElfConsensusHeaderInformation GetConsensusExtraDataToPublishOutValue(Round currentRound,
            string pubkey, AElfConsensusTriggerInformation triggerInformation)
        {
            currentRound.RealTimeMinersInformation[pubkey].ProducedTinyBlocks = currentRound
                .RealTimeMinersInformation[pubkey].ProducedTinyBlocks.Add(1);
            currentRound.RealTimeMinersInformation[pubkey].ProducedBlocks =
                currentRound.RealTimeMinersInformation[pubkey].ProducedBlocks.Add(1);
            currentRound.RealTimeMinersInformation[pubkey].ActualMiningTimes
                .Add(Context.CurrentBlockTime);

            Assert(triggerInformation.InValue != null, "Random hash should not be null.");

            var outValue = Hash.FromMessage(triggerInformation.InValue);
            var signature =
                Hash.FromTwoHashes(outValue, triggerInformation.InValue); // Just initial signature value.
            var previousInValue = Hash.Empty; // Just initial previous in value.

            if (TryToGetPreviousRoundInformation(out var previousRound) && !IsFirstRoundOfCurrentTerm(out _))
            {
                signature = previousRound.CalculateSignature(triggerInformation.InValue);
                Context.LogDebug(
                    () => $"Previous in value in trigger information: {triggerInformation.PreviousInValue}");
                if (triggerInformation.PreviousInValue != null &&
                    triggerInformation.PreviousInValue != Hash.Empty)
                {
                    // Self check.
                    if (Hash.FromMessage(triggerInformation.PreviousInValue) !=
                        previousRound.RealTimeMinersInformation[pubkey].OutValue)
                    {
                        Context.LogDebug(() => "Failed to produce block at previous round?");
                        previousInValue = Hash.Empty;
                    }
                    else
                    {
                        previousInValue = triggerInformation.PreviousInValue;
                    }
                }
            }

            var updatedRound = currentRound.ApplyNormalConsensusData(pubkey, previousInValue,
                outValue, signature);

            Context.LogDebug(
                () => $"Previous in value after ApplyNormalConsensusData: {updatedRound.RealTimeMinersInformation[pubkey].PreviousInValue}");

            updatedRound.RealTimeMinersInformation[pubkey].ImpliedIrreversibleBlockHeight = Context.CurrentHeight;

            // Update secret pieces of latest in value.
            foreach (var encryptedPiece in triggerInformation.EncryptedPieces)
            {
                updatedRound.RealTimeMinersInformation[pubkey].EncryptedPieces
                    .Add(encryptedPiece.Key, encryptedPiece.Value);
            }

            foreach (var decryptedPiece in triggerInformation.DecryptedPieces)
            {
                if (updatedRound.RealTimeMinersInformation.ContainsKey(decryptedPiece.Key))
                {
                    updatedRound.RealTimeMinersInformation[decryptedPiece.Key].DecryptedPieces[pubkey] =
                        decryptedPiece.Value;
                }
            }

            foreach (var revealedInValue in triggerInformation.RevealedInValues)
            {
                if (updatedRound.RealTimeMinersInformation.ContainsKey(revealedInValue.Key) &&
                    (updatedRound.RealTimeMinersInformation[revealedInValue.Key].PreviousInValue == Hash.Empty ||
                     updatedRound.RealTimeMinersInformation[revealedInValue.Key].PreviousInValue == null))
                {
                    updatedRound.RealTimeMinersInformation[revealedInValue.Key].PreviousInValue = revealedInValue.Value;
                }
            }

            // To publish Out Value.
            return new AElfConsensusHeaderInformation
            {
                SenderPubkey = pubkey.ToByteString(),
                Round = updatedRound,
                Behaviour = triggerInformation.Behaviour
            };
        }

        private AElfConsensusHeaderInformation GetConsensusExtraDataForTinyBlock(Round currentRound,
            string pubkey, AElfConsensusTriggerInformation triggerInformation)
        {
            currentRound.RealTimeMinersInformation[pubkey].ProducedTinyBlocks = currentRound
                .RealTimeMinersInformation[pubkey].ProducedTinyBlocks.Add(1);
            currentRound.RealTimeMinersInformation[pubkey].ProducedBlocks =
                currentRound.RealTimeMinersInformation[pubkey].ProducedBlocks.Add(1);
            currentRound.RealTimeMinersInformation[pubkey].ActualMiningTimes
                .Add(Context.CurrentBlockTime);

            return new AElfConsensusHeaderInformation
            {
                SenderPubkey = pubkey.ToByteString(),
                Round = currentRound.GetTinyBlockRound(pubkey),
                Behaviour = triggerInformation.Behaviour
            };
        }

        private AElfConsensusHeaderInformation GetConsensusExtraDataForNextRound(Round currentRound,
            string pubkey, AElfConsensusTriggerInformation triggerInformation)
        {
            if (!GenerateNextRoundInformation(currentRound, Context.CurrentBlockTime, out var nextRound))
            {
                Assert(false, "Failed to generate next round information.");
            }

            if (!nextRound.RealTimeMinersInformation.Keys.Contains(pubkey))
            {
                return new AElfConsensusHeaderInformation
                {
                    SenderPubkey = pubkey.ToByteString(),
                    Round = nextRound,
                    Behaviour = triggerInformation.Behaviour
                };
            }

            RevealSharedInValues(currentRound, pubkey);

            nextRound.RealTimeMinersInformation[pubkey].ProducedBlocks =
                nextRound.RealTimeMinersInformation[pubkey].ProducedBlocks.Add(1);
            Context.LogDebug(() => $"Mined blocks: {nextRound.GetMinedBlocks()}");
            nextRound.ExtraBlockProducerOfPreviousRound = pubkey;

            nextRound.RealTimeMinersInformation[pubkey].ProducedTinyBlocks = 1;
            nextRound.RealTimeMinersInformation[pubkey].ActualMiningTimes
                .Add(Context.CurrentBlockTime);

            return new AElfConsensusHeaderInformation
            {
                SenderPubkey = pubkey.ToByteString(),
                Round = nextRound,
                Behaviour = triggerInformation.Behaviour
            };
        }

        private AElfConsensusHeaderInformation GetConsensusExtraDataForNextTerm(string publicKey,
            AElfConsensusTriggerInformation triggerInformation)
        {
            var firstRoundOfNextTerm = GenerateFirstRoundOfNextTerm(publicKey, State.MiningInterval.Value);
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