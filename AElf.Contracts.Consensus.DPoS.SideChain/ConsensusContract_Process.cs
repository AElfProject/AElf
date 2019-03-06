using System;
using System.Collections.Generic;
using System.Linq;
using AElf.Common;
using AElf.Kernel;
using AElf.Types.CSharp;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Contracts.Consensus.DPoS.SideChain
{
    public partial class ConsensusContract
    {
        public bool TryToGetRoundNumber(out ulong roundNumber)
        {
            roundNumber = State.CurrentRoundNumberField.Value;
            return roundNumber != 0;
        }

        public bool TryToGetTermNumber(out ulong termNumber)
        {
            termNumber = State.CurrentTermNumberField.Value;
            return termNumber != 0;
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

        public bool TryToGetMiners(ulong termNumber, out Miners miners)
        {
            miners = State.MinersMap[termNumber.ToUInt64Value()];
            return miners != null;
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

        public bool IsMinerOfCurrentTerm(string publicKey)
        {
            if (TryToGetTermNumber(out var termNumber))
            {
                if (TryToGetMiners(termNumber, out var miners))
                {
                    return miners.PublicKeys.Contains(publicKey);
                }
            }

            return false;
        }

        private bool ValidateMinersList(Round round1, Round round2)
        {
            return true;

            // TODO:
            // If the miners are different, we need a further validation
            // to prove the missing (replaced) one should be kicked out.
        }

        private bool InValueIsNull(Round round)
        {
            return round.RealTimeMinersInformation.Values.All(m => m.InValue == null);
        }

        private bool RoundIdMatched(Round round)
        {
            if (TryToGetCurrentRoundInformation(out var currentRoundInStateDB))
            {
                return currentRoundInStateDB.RoundId == round.RoundId;
            }

            return false;
        }

        private bool NewOutValueFilled(Round round)
        {
            if (TryToGetCurrentRoundInformation(out var currentRoundInStateDB))
            {
                return currentRoundInStateDB.RealTimeMinersInformation.Values.Count(info => info.OutValue != null) + 1 ==
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