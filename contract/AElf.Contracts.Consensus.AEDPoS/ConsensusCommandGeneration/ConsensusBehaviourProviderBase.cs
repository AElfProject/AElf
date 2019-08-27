using AElf.Sdk.CSharp;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Contracts.Consensus.AEDPoS
{
    public partial class AEDPoSContract
    {
        public abstract class ConsensusBehaviourProviderBase : IConsensusBehaviourProvider
        {
            protected readonly Round _currentRound;
            protected readonly string _pubkey;
            protected readonly int _maximumBlocksCount;
            protected readonly Timestamp _currentBlockTime;

            protected readonly bool _isTimeSlotPassed;
            protected readonly MinerInRound _minerInRound;

            protected ConsensusBehaviourProviderBase(Round currentRound, string pubkey, int maximumBlocksCount, Timestamp currentBlockTime)
            {
                _currentRound = currentRound;
                _pubkey = pubkey;
                _maximumBlocksCount = maximumBlocksCount;
                _currentBlockTime = currentBlockTime;

                _isTimeSlotPassed = _currentRound.IsTimeSlotPassed(_pubkey, _currentBlockTime);
                _minerInRound = _currentRound.RealTimeMinersInformation[_pubkey];
            }

            public AElfConsensusBehaviour GetConsensusBehaviour()
            {
                if (!_currentRound.IsInMinerList(_pubkey))
                {
                    return AElfConsensusBehaviour.Nothing;
                }

                if (!_minerInRound.IsThisANewRoundForThisMiner())
                {
                    var behaviour = HandleMinerInNewRound();
                    if (behaviour != AElfConsensusBehaviour.Nothing)
                    {
                        return behaviour;
                    }
                }
                else if (!_isTimeSlotPassed)
                {
                    if (_minerInRound.ProducedTinyBlocks < _maximumBlocksCount)
                    {
                        return AElfConsensusBehaviour.TinyBlock;
                    }

                    if (_currentRound.ExtraBlockProducerOfPreviousRound == _pubkey &&
                        !_currentRound.IsMinerListJustChanged &&
                        _minerInRound.ProducedTinyBlocks < _maximumBlocksCount.Mul(2))
                    {
                        if (_currentRound.IsMinerListJustChanged && _minerInRound.ProducedTinyBlocks > _maximumBlocksCount.Add(1))
                        {
                            // Because NextTerm time slot only produces one block.
                            return AElfConsensusBehaviour.NextRound;
                        }
                        return AElfConsensusBehaviour.TinyBlock;
                    }
                }

                return GetConsensusBehaviourToTerminateCurrentRound();
            }

            /// <summary>
            /// If this miner come to a new round, normally, there are three possible behaviour:
            /// UpdateValue (most common)
            /// UpdateValueWithoutPreviousInValue (happens if current round is the first round of current term)
            /// TinyBlock (happens if this miner is mining blocks for extra block time slot of previous round)
            /// NextRound (only happens in first round)
            /// </summary>
            /// <returns></returns>
            private AElfConsensusBehaviour HandleMinerInNewRound()
            {
                if (_currentRound.RoundNumber == 1 && // For first round, the expected mining time is incorrect (due to configuration),
                    _minerInRound.Order != 1 && // so we'd better prevent miners' ain't first order (meanwhile he isn't boot miner) from mining fork blocks
                    _currentRound.FirstMiner().OutValue == null // by postpone their mining time
                )
                {
                    return AElfConsensusBehaviour.NextRound;
                }
                
                if (_currentRound.ExtraBlockProducerOfPreviousRound == _pubkey && // If this miner is extra block producer of previous round,
                    _currentBlockTime < _currentRound.GetStartTime() && // and currently the time is ahead of current round,
                    _minerInRound.ProducedTinyBlocks < _maximumBlocksCount // make this miner produce some tiny blocks.
                )
                {
                    return AElfConsensusBehaviour.TinyBlock;
                }

                if (_currentRound.IsMinerListJustChanged)
                {
                    return AElfConsensusBehaviour.UpdateValueWithoutPreviousInValue;
                }

                if (!_isTimeSlotPassed)
                {
                    return AElfConsensusBehaviour.UpdateValue;
                }

                return AElfConsensusBehaviour.Nothing;
            }

            protected abstract AElfConsensusBehaviour GetConsensusBehaviourToTerminateCurrentRound();
        }
    }
}