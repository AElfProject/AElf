using System;
using System.Linq;
using AElf.Kernel;
using AElf.Sdk.CSharp;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Contracts.Consensus.AEDPoS
{
    public partial class AEDPoSContract
    {
        /// <summary>
        /// Get next consensus behaviour of the caller based on current state.
        /// This method can be tested by testing GetConsensusCommand.
        /// </summary>
        /// <param name="publicKey"></param>
        /// <param name="dateTime"></param>
        /// <param name="currentRound">Return current round information to avoid unnecessary database access.</param>
        /// <returns></returns>
        private AElfConsensusBehaviour GetBehaviour(string publicKey, DateTime dateTime, out Round currentRound)
        {
            currentRound = null;

            if (!TryToGetCurrentRoundInformation(out currentRound) ||
                !currentRound.RealTimeMinersInformation.ContainsKey(publicKey))
            {
                return AElfConsensusBehaviour.Nothing;
            }

            var ableToGetPreviousRound = TryToGetPreviousRoundInformation(out var previousRound);
            var isTermJustChanged = IsJustChangedTerm(out var termNumber);
            var isTimeSlotPassed = currentRound.IsTimeSlotPassed(publicKey, dateTime, out var minerInRound);
            if (minerInRound.OutValue == null)
            {
                // Current miner hasn't produce block in current round before.

                if (!ableToGetPreviousRound && minerInRound.Order != 1 &&
                    currentRound.RealTimeMinersInformation.Values.First(m => m.Order == 1).OutValue == null)
                {
                    // In first round, if block of boot node not executed, don't produce block to
                    // avoid forks creating.
                    return AElfConsensusBehaviour.NextRound;
                }

                if (!ableToGetPreviousRound || isTermJustChanged)
                {
                    // Failed to get previous round information or just changed term.
                    return AElfConsensusBehaviour.UpdateValueWithoutPreviousInValue;
                }
                
                if (currentRound.ExtraBlockProducerOfPreviousRound == publicKey &&
                    dateTime < currentRound.GetStartTime() &&
                    minerInRound.ProducedTinyBlocks < AElfConsensusContractConstants.TinyBlocksNumber)
                {
                    return AElfConsensusBehaviour.TinyBlock;
                }

                if (!isTimeSlotPassed)
                {
                    // If this node not missed his time slot of current round.
                    return AElfConsensusBehaviour.UpdateValue;
                }
            }
            else if (minerInRound.ProducedTinyBlocks < AElfConsensusContractConstants.TinyBlocksNumber)
            {
                return AElfConsensusBehaviour.TinyBlock;
            }
            else if (currentRound.ExtraBlockProducerOfPreviousRound == publicKey &&
                     minerInRound.ProducedTinyBlocks < AElfConsensusContractConstants.TinyBlocksNumber.Mul(2))
            {
                return AElfConsensusBehaviour.TinyBlock;
            }

            // If this node missed his time slot, a command of terminating current round will be fired,
            // and the terminate time will based on the order of this node (to avoid conflicts).

            // Side chain will go next round directly.
            if (State.TimeEachTerm.Value == int.MaxValue)
            {
                return AElfConsensusBehaviour.NextRound;
            }

            // In first round, the blockchain start timestamp is incorrect.
            // We can return NextRound directly.
            if (currentRound.RoundNumber == 1)
            {
                return AElfConsensusBehaviour.NextRound;
            }

            Assert(TryToGetBlockchainStartTimestamp(out var blockchainStartTimestamp),
                "Failed to get blockchain start timestamp.");

            Context.LogDebug(() => $"Using start timestamp: {blockchainStartTimestamp}");

            // Calculate the approvals and make the judgement of changing term.
            var changeTerm =
                currentRound.IsTimeToChangeTerm(previousRound, blockchainStartTimestamp.ToDateTime(), termNumber,
                    State.TimeEachTerm.Value, (TimeUnit) State.BaseTimeUnit.Value);
            return changeTerm
                ? AElfConsensusBehaviour.NextTerm
                : AElfConsensusBehaviour.NextRound;
        }

        /// <summary>
        /// AElf Consensus Behaviour is changeable in this method.
        /// It's the situation this miner should skip his time slot more precisely.
        /// </summary>
        /// <param name="behaviour"></param>
        /// <param name="currentRound"></param>
        /// <param name="previousRound"></param>
        /// <param name="publicKey"></param>
        /// <param name="dateTime"></param>
        /// <returns></returns>
        private ConsensusCommand GetConsensusCommand(AElfConsensusBehaviour behaviour, Round currentRound,
            Round previousRound, string publicKey,
            DateTime dateTime)
        {
            var minerInRound = currentRound.RealTimeMinersInformation[publicKey];
            var miningInterval = currentRound.GetMiningInterval();
            var myOrder = currentRound.RealTimeMinersInformation[minerInRound.PublicKey].Order;
            var expectedMiningTime = currentRound.RealTimeMinersInformation[minerInRound.PublicKey].ExpectedMiningTime;

            int nextBlockMiningLeftMilliseconds;
            var hint = new AElfConsensusHint {Behaviour = behaviour}.ToByteString();

            var producedTinyBlocks = minerInRound.ProducedTinyBlocks;

            switch (behaviour)
            {
                case AElfConsensusBehaviour.UpdateValueWithoutPreviousInValue:

                    nextBlockMiningLeftMilliseconds = minerInRound.Order * miningInterval;
                    break;

                case AElfConsensusBehaviour.UpdateValue:
                    expectedMiningTime = expectedMiningTime.ToDateTime().ToTimestamp();
                    nextBlockMiningLeftMilliseconds =
                        (int) (expectedMiningTime.ToDateTime() - dateTime).TotalMilliseconds;
                    break;
                case AElfConsensusBehaviour.TinyBlock:
                    if (minerInRound.OutValue != null)
                    {
                        if (currentRound.ExtraBlockProducerOfPreviousRound != publicKey)
                        {
                            expectedMiningTime = expectedMiningTime.ToDateTime().AddMilliseconds(producedTinyBlocks
                                    .Mul(miningInterval).Div(AElfConsensusContractConstants.TinyBlocksNumber))
                                .ToTimestamp();
                        }
                        else
                        {
                            // EBP of previous round will produce double tiny blocks. This is for normal time slot of current round.
                            expectedMiningTime = expectedMiningTime.ToDateTime().AddMilliseconds(producedTinyBlocks
                                .Sub(AElfConsensusContractConstants.TinyBlocksNumber)
                                .Mul(miningInterval).Div(AElfConsensusContractConstants.TinyBlocksNumber)).ToTimestamp();
                        }
                    }
                    else if (previousRound != null)
                    {
                        // EBP of previous round will produce double tiny blocks. This is for extra time slot of previous round.
                        expectedMiningTime = previousRound.GetExtraBlockMiningTime().AddMilliseconds(producedTinyBlocks
                            .Mul(miningInterval).Div(AElfConsensusContractConstants.TinyBlocksNumber)).ToTimestamp();
                    }

                    nextBlockMiningLeftMilliseconds =
                        (int) (expectedMiningTime.ToDateTime() - dateTime).TotalMilliseconds;
                    break;
                case AElfConsensusBehaviour.NextRound:
                    nextBlockMiningLeftMilliseconds = currentRound.RoundNumber == 1
                        ? currentRound.RealTimeMinersInformation.Count * miningInterval + myOrder * miningInterval
                        : (int) (currentRound.ArrangeAbnormalMiningTime(minerInRound.PublicKey, dateTime).ToDateTime() -
                                 dateTime).TotalMilliseconds;
                    break;
                case AElfConsensusBehaviour.NextTerm:
                    nextBlockMiningLeftMilliseconds =
                        (int) (currentRound.ArrangeAbnormalMiningTime(minerInRound.PublicKey, dateTime).ToDateTime() -
                               dateTime).TotalMilliseconds;
                    break;
                default:
                    return new ConsensusCommand
                    {
                        ExpectedMiningTime = expectedMiningTime,
                        NextBlockMiningLeftMilliseconds = int.MaxValue,
                        LimitMillisecondsOfMiningBlock = int.MaxValue,
                        Hint = new AElfConsensusHint
                        {
                            Behaviour = AElfConsensusBehaviour.Nothing
                        }.ToByteString()
                    };
            }

            return new ConsensusCommand
            {
                ExpectedMiningTime = expectedMiningTime,
                NextBlockMiningLeftMilliseconds = nextBlockMiningLeftMilliseconds,
                LimitMillisecondsOfMiningBlock = miningInterval / AElfConsensusContractConstants.TinyBlocksNumber,
                Hint = hint
            };
        }

        private bool TryToGetBlockchainStartTimestamp(out Timestamp timestamp)
        {
            timestamp = State.BlockchainStartTimestamp.Value;
            return timestamp != null;
        }

        private bool IsJustChangedTerm(out long termNumber)
        {
            termNumber = 0;
            return TryToGetPreviousRoundInformation(out var previousRound) &&
                   TryToGetTermNumber(out termNumber) &&
                   previousRound.TermNumber != termNumber;
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

        private bool TryToGetCurrentRoundInformation(out Round roundInformation)
        {
            roundInformation = null;
            if (TryToGetRoundNumber(out var roundNumber))
            {
                roundInformation = State.Rounds[roundNumber];
                if (roundInformation != null)
                {
                    return true;
                }
            }

            return false;
        }

        private bool TryToGetPreviousRoundInformation(out Round previousRound)
        {
            previousRound = new Round();
            if (TryToGetRoundNumber(out var roundNumber))
            {
                if (roundNumber < 2)
                {
                    return false;
                }

                previousRound = State.Rounds[(roundNumber - 1)];
                return !previousRound.IsEmpty;
            }

            return false;
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
    }
}