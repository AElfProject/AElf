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

namespace AElf.Contracts.Consensus.DPoS
{
    // ReSharper disable InconsistentNaming
    public partial class ConsensusContract
    {
        #region InitialDPoS

        public override Empty InitialConsensus(Round input)
        {
            Assert(input.RoundNumber == 1, "Incorrect round information: invalid round number.");

            Assert(input.RealTimeMinersInformation.Any(), "Incorrect round information: no miner.");

            InitialSettings(input);

            SetAliases(input);

            input.BlockchainAge = 1;

            Assert(TryToAddRoundInformation(input), "Failed to add round information.");

            return new Empty();
        }

        private void SetAliases(Round round)
        {
            var index = 0;
            var aliases = DPoSContractConsts.InitialMinersAliases.Split(',');
            foreach (var publicKey in round.RealTimeMinersInformation.Keys)
            {
                if (index >= aliases.Length)
                    return;

                var alias = aliases[index];
                SetAlias(publicKey, alias);
                index++;
            }
        }

        public void SetAlias(string publicKey, string alias)
        {
            State.AliasesMap[publicKey.ToStringValue()] = alias.ToStringValue();
            State.AliasesLookupMap[alias.ToStringValue()] = publicKey.ToStringValue();
        }

        #endregion

        #region UpdateValue

        public override Empty UpdateValue(ToUpdate input)
        {
            Assert(TryToGetCurrentRoundInformation(out var currentRound) &&
                   input.RoundId == currentRound.RoundId, "Round Id not matched.");

            Assert(TryToGetCurrentRoundInformation(out var round), "Round information not found.");

            var publicKey = Context.RecoverPublicKey().ToHex();

            if (round.RoundNumber != 1)
            {
                round.RealTimeMinersInformation[publicKey].Signature = input.Signature;
            }

            round.RealTimeMinersInformation[publicKey].OutValue = input.OutValue;

            round.RealTimeMinersInformation[publicKey].ProducedBlocks += 1;

            round.RealTimeMinersInformation[publicKey].PromisedTinyBlocks = input.PromiseTinyBlocks;
            
            round.RealTimeMinersInformation[publicKey].ActualMiningTime = input.ActualMiningTime;
            
            round.RealTimeMinersInformation[publicKey].OrderOfNextRound = input.OrderOfNextRound;

            foreach (var changeOrderInformation in input.ChangedOrders)
            {
                round.RealTimeMinersInformation[changeOrderInformation.PublickKey].OrderOfNextRound =
                    changeOrderInformation.NewOrder;
            }
            
            // One cannot publish his in value sometime, like in his first round.
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
            if (TryToGetRoundNumber(out var roundNumber))
            {
                Assert(roundNumber < input.RoundNumber, "Incorrect round number for next round.");
            }

            var senderPublicKey = Context.RecoverPublicKey().ToHex();

            input.ExtraBlockProducerOfPreviousRound = senderPublicKey;

            // Update the age of this blockchain
            State.AgeField.Value = input.BlockchainAge;

            Assert(TryToGetCurrentRoundInformation(out _), "Failed to get current round information.");

            UpdateHistoryInformation(input);

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

        public void TryToFindLIB()
        {
            if (CalculateLIB(out var offset))
            {
                Context.LogDebug(() => $"LIB found, offset is {offset}");
                Context.FireEvent(new LIBFound
                {
                    Offset = offset
                });
            }
        }

        public override SInt64Value GetLIBOffset(Empty input)
        {
            return new SInt64Value {Value = CalculateLIB(out var offset) ? offset : 0};
        }

        private bool CalculateLIB(out long offset)
        {
            offset = 0;

            if (TryToGetCurrentRoundInformation(out var currentRound))
            {
                var currentRoundMiners = currentRound.RealTimeMinersInformation;

                var minersCount = currentRoundMiners.Count;

                var minimumCount = ((int) ((minersCount * 2d) / 3)) + 1;

                if (minersCount == 1)
                {
                    // Single node will set every previous block as LIB.
                    offset = 1;
                    return true;
                }

                var validMinersOfCurrentRound = currentRoundMiners.Values.Where(m => m.OutValue != null).ToList();
                var validMinersCountOfCurrentRound = validMinersOfCurrentRound.Count;

                var senderPublicKey = Context.RecoverPublicKey().ToHex();
                if (validMinersCountOfCurrentRound >= minimumCount)
                {
                    offset = minimumCount;
                    return true;
                }

                // Current round is not enough to find LIB.

                var publicKeys = new HashSet<string>(validMinersOfCurrentRound.Select(m => m.PublicKey));

                if (TryToGetPreviousRoundInformation(out var previousRound))
                {
                    var preRoundMiners = previousRound.RealTimeMinersInformation.Values.OrderByDescending(m => m.Order)
                        .Select(m => m.PublicKey).ToList();

                    var traversalBlocksCount = publicKeys.Count;

                    for (var i = 0; i < minersCount; i++)
                    {
                        if (++traversalBlocksCount > minersCount)
                        {
                            return false;
                        }

                        var miner = preRoundMiners[i];

                        if (previousRound.RealTimeMinersInformation[miner].OutValue != null)
                        {
                            if (!publicKeys.Contains(miner))
                                publicKeys.Add(miner);
                        }

                        if (publicKeys.Count >= minimumCount)
                        {
                            offset = minimumCount;
                            return true;
                        }
                    }
                }
            }

            return false;
        }

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

        public bool TryToGetPreviousRoundInformation(out Round roundInformation)
        {
            if (TryToGetRoundNumber(out var roundNumber))
            {
                roundInformation = State.RoundsMap[(roundNumber - 1).ToInt64Value()];
                if (roundInformation != null)
                {
                    return true;
                }
            }

            roundInformation = new Round();
            return false;
        }

        public bool TryToGetRoundInformation(long roundNumber, out Round roundInformation)
        {
            roundInformation = State.RoundsMap[roundNumber.ToInt64Value()];
            return roundInformation != null;
        }

        public bool TryToGetMiningInterval(out int miningInterval)
        {
            miningInterval = State.MiningIntervalField.Value;
            return miningInterval > 0;
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