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
        private bool IsMainChain
        {
            get
            {
                if (_isMainChain != null) return (bool) _isMainChain;
                _isMainChain = State.IsMainChain.Value;
                return (bool) _isMainChain;
            }
        }

        private Timestamp GetBlockchainStartTimestamp()
        {
            return State.BlockchainStartTimestamp.Value ?? new Timestamp();
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

        private bool TryToGetCurrentRoundInformation(out Round round, bool useCache = false)
        {
            round = null;
            if (!TryToGetRoundNumber(out var roundNumber)) return false;

            if (useCache && _rounds.ContainsKey(roundNumber))
            {
                round = _rounds[roundNumber];
            }
            else
            {
                round = State.Rounds[roundNumber];
            }

            return !round.IsEmpty;
        }

        private bool TryToGetPreviousRoundInformation(out Round previousRound, bool useCache = false)
        {
            previousRound = new Round();
            if (!TryToGetRoundNumber(out var roundNumber)) return false;
            if (roundNumber < 2) return false;
            var targetRoundNumber = roundNumber.Sub(1);
            if (useCache && _rounds.ContainsKey(targetRoundNumber))
            {
                previousRound = _rounds[targetRoundNumber];
            }
            else
            {
                previousRound = State.Rounds[targetRoundNumber];
            }

            return !previousRound.IsEmpty;
        }

        private bool TryToGetRoundInformation(long roundNumber, out Round round, bool useCache = false)
        {
            if (useCache && _rounds.ContainsKey(roundNumber))
            {
                round = _rounds[roundNumber];
            }
            else
            {
                round = State.Rounds[roundNumber];
                _rounds[roundNumber] = round;
            }

            return !round.IsEmpty;
        }

        private Transaction GenerateTransaction(string methodName, IMessage parameter) => new Transaction
        {
            From = Context.Sender,
            To = Context.Self,
            MethodName = methodName,
            Params = parameter.ToByteString(),
            RefBlockNumber = Context.CurrentHeight,
            RefBlockPrefix = ByteString.CopyFrom(Context.PreviousBlockHash.Value.Take(4).ToArray())
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

        /// <summary>
        /// Will force to generate a `Change` to tx executing result.
        /// </summary>
        /// <param name="round"></param>
        /// <returns></returns>
        private bool TryToAddRoundInformation(Round round)
        {
            State.Rounds.Set(round.RoundNumber, round);

            if (round.RoundNumber > 1 && !round.IsMinerListJustChanged)
            {
                // No need to share secret pieces if miner list just changed.

                Context.Fire(new SecretSharingInformation
                {
                    CurrentRoundId = round.RoundId,
                    PreviousRound = State.Rounds[round.RoundNumber.Sub(1)],
                    PreviousRoundId = State.Rounds[round.RoundNumber.Sub(1)].RoundId
                });
            }

            // Only clear old round information when the mining status is Normal.
            var roundNumberToRemove = round.RoundNumber.Sub(AEDPoSContractConstants.KeepRounds);
            if (
                roundNumberToRemove >
                1 && // Which means we won't remove the information of the first round of first term.
                GetMaximumBlocksCount() == AEDPoSContractConstants.MaximumTinyBlocksCount)
            {
                State.Rounds.Remove(roundNumberToRemove);
            }

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