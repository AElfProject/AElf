using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using AElf.Common;
using AElf.Kernel;
using AElf.Sdk.CSharp;
using AElf.Types.CSharp;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Contracts.Consensus.DPoS
{
    // ReSharper disable InconsistentNaming
    public partial class ConsensusContract
    {
        #region InitialDPoS
        
        public void InitialConsensus(Round firstRound)
        {
            Assert(firstRound.RoundNumber == 1,
                "It seems that the term number of initial term is incorrect.");

            Assert(firstRound.RealTimeMinersInformation.Any(), "No miners in round information.");
            
            InitialSettings(firstRound);

            SetAliases(firstRound);

            firstRound.BlockchainAge = 1;
            Assert(TryToAddRoundInformation(firstRound), "Failed to add round information.");
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
        
        public void UpdateValue(ToUpdate toUpdate)
        {
            Assert(TryToGetCurrentRoundInformation(out var currentRound) &&
                   toUpdate.RoundId == currentRound.RoundId, DPoSContractConsts.RoundIdNotMatched);

            Assert(TryToGetCurrentRoundInformation(out var round), "Round information not found.");

            var publicKey = Context.RecoverPublicKey().ToHex();

            if (round.RoundNumber != 1)
            {
                round.RealTimeMinersInformation[publicKey].Signature = toUpdate.Signature;
            }

            round.RealTimeMinersInformation[publicKey].OutValue = toUpdate.OutValue;

            round.RealTimeMinersInformation[publicKey].ProducedBlocks += 1;

            round.RealTimeMinersInformation[publicKey].PromisedTinyBlocks = toUpdate.PromiseTinyBlocks;

            // One cannot publish his in value sometime, like in his first round.
            if (toUpdate.PreviousInValue != Hash.Default)
            {
                round.RealTimeMinersInformation[publicKey].PreviousInValue = toUpdate.PreviousInValue;
            }
            
            Assert(TryToUpdateRoundInformation(round), "Failed to update round information.");

            TryToFindLIB();
        }
        #endregion

        #region NextRound

        public void NextRound(Round round)
        {
            if (TryToGetRoundNumber(out var roundNumber))
            {
                Assert(roundNumber < round.RoundNumber, "Incorrect round number for next round.");
            }

            var senderPublicKey = Context.RecoverPublicKey().ToHex();

            round.ExtraBlockProducerOfPreviousRound = senderPublicKey;

            // Update the age of this blockchain
            State.AgeField.Value = round.BlockchainAge;

            Assert(TryToGetCurrentRoundInformation(out var currentRound), "Failed to get current round information.");

            UpdateHistoryInformation(round);

            Assert(TryToAddRoundInformation(round), "Failed to add round information.");
            Assert(TryToUpdateRoundNumber(round.RoundNumber), "Failed to update round number.");

            TryToFindLIB();
        }
        
        private bool TryToUpdateRoundNumber(ulong roundNumber)
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
                    offset = 1;
                    return true;
                }
                
                var validMinersOfCurrentRound = currentRoundMiners.Values.Where(m => m.OutValue != null).ToList();
                var validMinersCountOfCurrentRound = validMinersOfCurrentRound.Count;

                var senderPublicKey = Context.RecoverPublicKey().ToHex();
                var senderOrder = currentRoundMiners[senderPublicKey].Order;
                if (validMinersCountOfCurrentRound > minimumCount)
                {
                    offset = senderOrder;
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
                            offset = validMinersCountOfCurrentRound +  i;
                            return true;
                        }
                    }
                }
            }

            return false;
        }

         private bool TryToAddRoundInformation(Round round)
        {
            var ri = State.RoundsMap[round.RoundNumber.ToUInt64Value()];
            if (ri != null)
            {
                return false;
            }

            State.RoundsMap[round.RoundNumber.ToUInt64Value()] = round;
            return true;
        }

        private bool TryToUpdateRoundInformation(Round round)
        {
            var ri = State.RoundsMap[round.RoundNumber.ToUInt64Value()];
            if (ri == null)
            {
                return false;
            }

            State.RoundsMap[round.RoundNumber.ToUInt64Value()] = round;
            return true;
        }
        
        public bool TryToGetRoundNumber(out ulong roundNumber)
        {
            roundNumber = State.CurrentRoundNumberField.Value;
            return roundNumber != 0;
        }

        public bool TryToGetCurrentRoundInformation(out Round roundInformation)
        {
            roundInformation = null;
            if (TryToGetRoundNumber(out var roundNumber))
            {
                roundInformation = State.RoundsMap[roundNumber.ToUInt64Value()];
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
                roundInformation = State.RoundsMap[(roundNumber - 1).ToUInt64Value()];
                if (roundInformation != null)
                {
                    return true;
                }
            }

            roundInformation = new Round();
            return false;
        }

        public bool TryToGetRoundInformation(ulong roundNumber, out Round roundInformation)
        {
            roundInformation = State.RoundsMap[roundNumber.ToUInt64Value()];
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

        private bool NewOutValueFilled(Round round)
        {
            if (TryToGetCurrentRoundInformation(out var currentRoundInStateDatabase))
            {
                return currentRoundInStateDatabase.RealTimeMinersInformation.Values.Count(info => info.OutValue != null) + 1 ==
                       round.RealTimeMinersInformation.Values.Count(info => info.OutValue != null);
            }

            return false;
        }

        private Transaction GenerateTransaction(string methodName, List<object> parameters)
        {
            var tx = new Transaction
            {
                From = Context.Sender,
                To = Context.Self,
                MethodName = methodName,
                Type = TransactionType.DposTransaction,
                Params = ByteString.CopyFrom(ParamsPacker.Pack(parameters.ToArray()))
            };

            return tx;
        }
    }
}