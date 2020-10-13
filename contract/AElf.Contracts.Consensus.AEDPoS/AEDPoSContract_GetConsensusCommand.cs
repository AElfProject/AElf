using System.Linq;
using AElf.Standards.ACS4;
using AElf.CSharp.Core;
using AElf.Sdk.CSharp;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Contracts.Consensus.AEDPoS
{
    // ReSharper disable once InconsistentNaming
    public partial class AEDPoSContract
    {
        /// <summary>
        /// AElf Consensus Behaviour is changeable in this method when
        /// this miner should skip his time slot more precisely.
        /// </summary>
        /// <param name="behaviour"></param>
        /// <param name="currentRound"></param>
        /// <param name="pubkey"></param>
        /// <param name="currentBlockTime"></param>
        /// <returns></returns>
        private ConsensusCommand GetConsensusCommand(AElfConsensusBehaviour behaviour, Round currentRound,
            string pubkey, Timestamp currentBlockTime = null)
        {
            if (SolitaryMinerDetection(currentRound, pubkey))
                return ConsensusCommandProvider.InvalidConsensusCommand;

            Context.LogDebug(() => $"Params to get command: {behaviour}, {pubkey}, {currentBlockTime}");

            if (currentRound.RoundNumber == 1 && behaviour == AElfConsensusBehaviour.UpdateValue)
                return new ConsensusCommandProvider(new FirstRoundCommandStrategy(currentRound, pubkey,
                    currentBlockTime, behaviour)).GetConsensusCommand();

            switch (behaviour)
            {
                case AElfConsensusBehaviour.UpdateValue:
                    TryToGetPreviousRoundInformation(out var previousRound);
                    return new ConsensusCommandProvider(new NormalBlockCommandStrategy(currentRound, pubkey,
                        currentBlockTime, previousRound.RoundId)).GetConsensusCommand();

                case AElfConsensusBehaviour.NextRound:
                case AElfConsensusBehaviour.NextTerm:
                    return new ConsensusCommandProvider(
                            new TerminateRoundCommandStrategy(currentRound, pubkey, currentBlockTime,
                                behaviour == AElfConsensusBehaviour.NextTerm))
                        .GetConsensusCommand();

                case AElfConsensusBehaviour.TinyBlock:
                {
                    var consensusCommand =
                        new ConsensusCommandProvider(new TinyBlockCommandStrategy(currentRound, pubkey,
                            currentBlockTime, GetMaximumBlocksCount())).GetConsensusCommand();
                    return consensusCommand;
                }

                default:
                    return ConsensusCommandProvider.InvalidConsensusCommand;
            }
        }

        /// <summary>
        /// If current miner mined blocks only himself for 2 rounds,
        /// just stop and waiting to execute other miners' blocks.
        /// </summary>
        /// <param name="currentRound"></param>
        /// <param name="pubkey"></param>
        /// <returns></returns>
        private bool SolitaryMinerDetection(Round currentRound, string pubkey)
        {
            var isAlone = false;
            // Skip this detection until 4th round.
            if (currentRound.RoundNumber > 3 && currentRound.RealTimeMinersInformation.Count > 2)
            {
                // Not single node.

                var minedMinersOfCurrentRound = currentRound.GetMinedMiners();
                isAlone = minedMinersOfCurrentRound.Count == 0;

                // If only this node mined during previous round, stop mining.
                if (TryToGetPreviousRoundInformation(out var previousRound) && isAlone)
                {
                    var minedMiners = previousRound.GetMinedMiners();
                    isAlone = minedMiners.Count == 1 &&
                              minedMiners.Select(m => m.Pubkey).Contains(pubkey);
                }

                // check one further round.
                if (isAlone && TryToGetRoundInformation(previousRound.RoundNumber.Sub(1),
                        out var previousPreviousRound))
                {
                    var minedMiners = previousPreviousRound.GetMinedMiners();
                    isAlone = minedMiners.Count == 1 &&
                              minedMiners.Select(m => m.Pubkey).Contains(pubkey);
                }
            }

            return isAlone;
        }
    }
}