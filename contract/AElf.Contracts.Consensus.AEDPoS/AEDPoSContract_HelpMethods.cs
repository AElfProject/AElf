using System;
using System.Linq;
using AElf.Sdk.CSharp;
using AElf.Types;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Contracts.Consensus.AEDPoS
{
    public partial class AEDPoSContract
    {
        private bool TryToGetBlockchainStartTimestamp(out Timestamp startTimestamp)
        {
            startTimestamp = State.BlockchainStartTimestamp.Value;
            return startTimestamp != null;
        }

        private bool IsFirstRoundOfCurrentTerm(out long termNumber)
        {
            termNumber = 1;
            return TryToGetTermNumber(out termNumber) &&
                   TryToGetPreviousRoundInformation(out var previousRound) &&
                   previousRound.TermNumber != termNumber ||
                   TryToGetRoundNumber(out var roundNumber) && roundNumber == 1;
        }

        private bool TryToGetTermNumber(out long termNumber)
        {
            termNumber = State.CurrentTermNumber.Value;
            return termNumber != 0;
        }

        private bool TryToGetRoundNumber(out long roundNumber)
        {
            roundNumber = State.CurrentRoundNumber.Value;
            return roundNumber != 0;
        }

        private bool TryToGetCurrentRoundInformation(out Round round)
        {
            round = null;
            if (!TryToGetRoundNumber(out var roundNumber)) return false;
            round = State.Rounds[roundNumber];
            return !round.IsEmpty;
        }

        private bool TryToGetPreviousRoundInformation(out Round previousRound)
        {
            previousRound = new Round();
            if (!TryToGetRoundNumber(out var roundNumber)) return false;
            if (roundNumber < 2) return false;
            previousRound = State.Rounds[roundNumber - 1];
            return !previousRound.IsEmpty;
        }

        private bool TryToGetRoundInformation(long roundNumber, out Round roundInformation)
        {
            roundInformation = State.Rounds[roundNumber];
            return roundInformation != null;
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

        private Duration ConvertMillisecondsToDuration(int milliseconds)
        {
            var seconds = 0L;
            if (milliseconds.Sub(1000) > 0)
            {
                seconds = milliseconds.Div(1000);
            }

            var nanos = (milliseconds % 1000).Mul(1000000);

            return new Duration {Seconds = seconds, Nanos = nanos};
        }

        private int GetNextBlockMiningLeftMillisecondsForPreviousRoundExtraBlockProducer(
            Timestamp previousExtraBlockTimestamp, int producedTinyBlocks, Timestamp blockTime)
        {
            var timeForEachBlock = State.MiningInterval.Value.Div(AEDPoSContractConstants.TotalTinySlots);
            var expectedMiningTime = previousExtraBlockTimestamp.AddMilliseconds(timeForEachBlock.Mul(producedTinyBlocks));
            var leftMilliseconds = (int) (expectedMiningTime - blockTime).Milliseconds();
            return leftMilliseconds;
        }

        private void SetBlockchainStartTimestamp(Timestamp timestamp)
        {
            Context.LogDebug(() => $"Set start timestamp to {timestamp}");
            State.BlockchainStartTimestamp.Value = timestamp;
        }

        private bool TryToUpdateRoundNumber(long roundNumber)
        {
            var oldRoundNumber = State.CurrentRoundNumber.Value;
            if (roundNumber != 1 && oldRoundNumber + 1 != roundNumber)
            {
                return false;
            }

            State.CurrentRoundNumber.Value = roundNumber;
            return true;
        }

        private bool TryToAddRoundInformation(Round round)
        {
            var ri = State.Rounds[round.RoundNumber];
            if (ri != null)
            {
                return false;
            }

            State.Rounds[round.RoundNumber] = round;
            return true;
        }

        private bool TryToUpdateRoundInformation(Round round)
        {
            var ri = State.Rounds[round.RoundNumber];
            if (ri == null)
            {
                return false;
            }

            State.Rounds[round.RoundNumber] = round;
            return true;
        }
    }
}