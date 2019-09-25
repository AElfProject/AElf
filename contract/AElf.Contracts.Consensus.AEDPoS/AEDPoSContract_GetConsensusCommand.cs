using System.Linq;
using Acs4;
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
        /// <param name="publicKey"></param>
        /// <returns></returns>
        private ConsensusCommand GetConsensusCommand(AElfConsensusBehaviour behaviour, Round currentRound,
            string publicKey)
        {
            if (SolitaryMinerDetection(currentRound, publicKey))
                return ConsensusCommandProvider.InvalidConsensusCommand;

            var currentBlockTime = Context.CurrentBlockTime;

            if (currentRound.RoundNumber == 1 && behaviour != AElfConsensusBehaviour.TinyBlock)
                return new ConsensusCommandProvider(new FirstRoundCommandStrategy(currentRound, publicKey,
                    currentBlockTime, behaviour)).GetConsensusCommand();

            switch (behaviour)
            {
                case AElfConsensusBehaviour.UpdateValueWithoutPreviousInValue:
                case AElfConsensusBehaviour.UpdateValue:
                    return new ConsensusCommandProvider(new NormalBlockCommandStrategy(currentRound, publicKey,
                        currentBlockTime)).GetConsensusCommand();

                case AElfConsensusBehaviour.NextRound:
                case AElfConsensusBehaviour.NextTerm:
                    return new ConsensusCommandProvider(
                            new TerminateRoundCommandStrategy(currentRound, publicKey, currentBlockTime,
                                behaviour == AElfConsensusBehaviour.NextTerm))
                        .GetConsensusCommand();

                case AElfConsensusBehaviour.TinyBlock:
                {
                    var consensusCommand =
                        new ConsensusCommandProvider(new TinyBlockCommandStrategy(currentRound, publicKey,
                            currentBlockTime, GetMaximumBlocksCount())).GetConsensusCommand();
                    if (consensusCommand.Hint ==
                        new AElfConsensusHint {Behaviour = AElfConsensusBehaviour.NextRound}.ToByteString())
                    {
                        Context.LogDebug(() => "Re-arranged behaviour from TinyBlock to NextRound.");
                    }

                    return consensusCommand;
                }
            }

            return ConsensusCommandProvider.InvalidConsensusCommand;
        }

        /// <summary>
        /// If current miner mined blocks only himself for 2 rounds,
        /// just stop and waiting to execute other miners' blocks.
        /// </summary>
        /// <param name="currentRound"></param>
        /// <param name="publicKey"></param>
        /// <returns></returns>
        private bool SolitaryMinerDetection(Round currentRound, string publicKey)
        {
            var isAlone = false;
            // Skip this detection until 4th round.
            if (currentRound.RoundNumber > 3 && currentRound.RealTimeMinersInformation.Count > 2)
            {
                // Not single node.

                // If only this node mined during previous round, stop mining.
                if (TryToGetPreviousRoundInformation(out var previousRound))
                {
                    var minedMiners = previousRound.GetMinedMiners();
                    isAlone = minedMiners.Count == 1 &&
                              minedMiners.Select(m => m.Pubkey).Contains(publicKey);
                }

                // check one further round.
                if (isAlone && TryToGetRoundInformation(previousRound.RoundNumber.Sub(1),
                        out var previousPreviousRound))
                {
                    var minedMiners = previousPreviousRound.GetMinedMiners();
                    isAlone = minedMiners.Count == 1 &&
                              minedMiners.Select(m => m.Pubkey).Contains(publicKey);
                }
            }

            return isAlone;
        }
    }
}