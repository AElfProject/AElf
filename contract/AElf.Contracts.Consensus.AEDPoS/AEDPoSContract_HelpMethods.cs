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
            if (!TryToGetRoundNumber(out var roundNumber))
            {
                Context.LogDebug(() => "Failed to get current round number.");
                return false;
            }
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

        private bool TryToGetRoundInformation(long roundNumber, out Round round)
        {
            round = State.Rounds[roundNumber];
            return !round.IsEmpty;
        }

        private Transaction GenerateTransaction(string methodName, IMessage parameter) => new Transaction
        {
            From = Context.Sender,
            To = Context.Self,
            MethodName = methodName,
            Params = parameter.ToByteString()
        };

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
                Context.LogDebug(() => "Round information not found");
                return false;
            }

            State.Rounds[round.RoundNumber] = round;
            return true;
        }
    }
}