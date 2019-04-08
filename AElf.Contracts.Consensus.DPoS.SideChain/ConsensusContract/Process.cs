using System.Collections.Generic;
using System.Linq;
using AElf.Common;
using AElf.Consensus.DPoS;
using AElf.Kernel;
using AElf.Sdk.CSharp;
using AElf.Types.CSharp;
using Google.Protobuf;
using Google.Protobuf.Collections;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Contracts.Consensus.DPoS.SideChain
{
    // ReSharper disable InconsistentNaming
    public partial class ConsensusContract
    {
        #region UpdateValue

        public override Empty UpdateValue(ToUpdate input)
        {
            Assert(TryToGetCurrentRoundInformation(out var round), "Round information not found.");

            Assert(input.RoundId == round.RoundId, "Round Id not matched.");

            var publicKey = Context.RecoverPublicKey().ToHex();

            round.RealTimeMinersInformation[publicKey].Signature = input.Signature;
            round.RealTimeMinersInformation[publicKey].OutValue = input.OutValue;
            round.RealTimeMinersInformation[publicKey].PromisedTinyBlocks = input.PromiseTinyBlocks;
            round.RealTimeMinersInformation[publicKey].ActualMiningTime = input.ActualMiningTime;
            round.RealTimeMinersInformation[publicKey].SupposedOrderOfNextRound = input.SupposedOrderOfNextRound;
            round.RealTimeMinersInformation[publicKey].FinalOrderOfNextRound = input.SupposedOrderOfNextRound;
            round.RealTimeMinersInformation[publicKey].ProducedBlocks = input.ProducedBlocks;

            round.RealTimeMinersInformation[publicKey].EncryptedInValues.Add(input.EncryptedInValues);
            foreach (var decryptedPreviousInValue in input.DecryptedPreviousInValues)
            {
                round.RealTimeMinersInformation[decryptedPreviousInValue.Key].DecryptedPreviousInValues
                    .Add(publicKey, decryptedPreviousInValue.Value);
            }

            foreach (var previousInValue in input.MinersPreviousInValues)
            {
                if (previousInValue.Key == publicKey)
                {
                    continue;
                }

                var filledValue = round.RealTimeMinersInformation[previousInValue.Key].PreviousInValue;
                if (filledValue != null && filledValue != previousInValue.Value)
                {
                    Context.LogDebug(() => $"Something wrong happened to previous in value of {previousInValue.Key}.");
                }

                round.RealTimeMinersInformation[previousInValue.Key].PreviousInValue = previousInValue.Value;
            }

            foreach (var tuneOrder in input.TuneOrderInformation)
            {
                LogVerbose(
                    $"Will tune {tuneOrder.Key} order from {round.RealTimeMinersInformation[tuneOrder.Key].FinalOrderOfNextRound} to {tuneOrder.Value}");
                round.RealTimeMinersInformation[tuneOrder.Key].FinalOrderOfNextRound = tuneOrder.Value;
            }

            LogVerbose($"Previous in value published by {publicKey} himself is {input.PreviousInValue.ToHex()}");
            // For first round of each term, no one need to publish in value.
            if (input.PreviousInValue != Hash.Empty)
            {
                round.RealTimeMinersInformation[publicKey].PreviousInValue = input.PreviousInValue;
            }

            Assert(TryToUpdateRoundInformation(round), "Failed to update round information.");

            TryToFindLIB();
            return new Empty();
        }

        #endregion

        #region NextRound

        public override Empty NextRound(Round input)
        {
            if (TryToGetRoundNumber(out var currentRoundNumber))
            {
                Assert(currentRoundNumber < input.RoundNumber, "Incorrect round number for next round.");
            }

            // Update the age of this blockchain
            UpdateBlockchainAge(input.BlockchainAge);

            Assert(TryToGetCurrentRoundInformation(out _), "Failed to get current round information.");
            //UpdateHistoryInformation(input);
            Assert(TryToAddRoundInformation(input), "Failed to add round information.");
            Assert(TryToUpdateRoundNumber(input.RoundNumber), "Failed to update round number.");
            TryToFindLIB();
            return new Empty();
        }

        private bool TryToUpdateRoundNumber(long roundNumber)
        {
            var oldRoundNumber = State.CurrentRoundNumberField.Value;
            if (roundNumber != 1 && oldRoundNumber + 1 != roundNumber)
            {
                return false;
            }

            State.CurrentRoundNumberField.Value = roundNumber;
            return true;
        }

        #endregion

        private bool TryToAddRoundInformation(Round round)
        {
            var ri = State.RoundsMap[round.RoundNumber.ToInt64Value()];
            if (ri != null)
            {
                return false;
            }

            State.RoundsMap[round.RoundNumber.ToInt64Value()] = round;
            return true;
        }

        private bool TryToUpdateRoundInformation(Round round)
        {
            var ri = State.RoundsMap[round.RoundNumber.ToInt64Value()];
            if (ri == null)
            {
                return false;
            }

            State.RoundsMap[round.RoundNumber.ToInt64Value()] = round;
            return true;
        }

        public bool TryToGetRoundNumber(out long roundNumber)
        {
            roundNumber = State.CurrentRoundNumberField.Value;
            return roundNumber != 0;
        }

        public bool TryToGetCurrentRoundInformation(out Round roundInformation)
        {
            roundInformation = null;
            if (TryToGetRoundNumber(out var roundNumber))
            {
                roundInformation = State.RoundsMap[roundNumber.ToInt64Value()];
                if (roundInformation != null)
                {
                    return true;
                }
            }

            return false;
        }

        public bool TryToGetPreviousRoundInformation(out Round previousRound)
        {
            previousRound = new Round();
            if (TryToGetRoundNumber(out var roundNumber))
            {
                if (roundNumber < 2)
                {
                    return false;
                }

                previousRound = State.RoundsMap[(roundNumber - 1).ToInt64Value()];
                return !previousRound.IsEmpty();
            }

            return false;
        }

        public bool TryToGetBlockchainStartTimestamp(out Timestamp timestamp)
        {
            timestamp = State.BlockchainStartTimestamp.Value;
            return timestamp != null;
        }

        private bool InValueIsNull(Round round)
        {
            return round.RealTimeMinersInformation.Values.All(m => m.InValue == null);
        }

        private bool RoundIdMatched(Round round)
        {
            if (TryToGetCurrentRoundInformation(out var currentRoundInStateDatabase))
            {
                return currentRoundInStateDatabase.RoundId == round.RoundId;
            }

            return false;
        }

        /// <summary>
        /// Check only one Out Value was filled during this updating.
        /// </summary>
        /// <param name="minersInformation"></param>
        /// <returns></returns>
        private bool NewOutValueFilled(IEnumerable<MinerInRound> minersInformation)
        {
            if (TryToGetCurrentRoundInformation(out var currentRound))
            {
                return currentRound.RealTimeMinersInformation.Values.Count(info => info.OutValue != null) + 1 ==
                       minersInformation.Count(info => info.OutValue != null);
            }

            return false;
        }

        private Transaction GenerateTransaction(string methodName, IMessage parameter)
        {
            var tx = new Transaction
            {
                From = Context.Sender,
                To = Context.Self,
                MethodName = methodName,
                Params = parameter.ToByteString()
            };

            return tx;
        }
    }
}