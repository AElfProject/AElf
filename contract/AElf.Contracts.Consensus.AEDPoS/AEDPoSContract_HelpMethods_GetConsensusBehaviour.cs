using AElf.Sdk.CSharp;

namespace AElf.Contracts.Consensus.AEDPoS
{
    public partial class AEDPoSContract
    {
        /// <summary>
        /// Get next consensus behaviour of the provided public key based on current state.
        /// </summary>
        /// <param name="currentRound"></param>
        /// <param name="publicKey"></param>
        /// <returns></returns>
        private AElfConsensusBehaviour GetConsensusBehaviour(Round currentRound, string publicKey)
        {
            if (!IsInCurrentMinerList(currentRound, publicKey)) return AElfConsensusBehaviour.Nothing;

            var isFirstRoundOfCurrentTerm = IsFirstRoundOfCurrentTerm(out var termNumber);
            var isTimeSlotPassed = currentRound.IsTimeSlotPassed(publicKey, Context.CurrentBlockTime);

            var minerInRound = currentRound.RealTimeMinersInformation[publicKey];
            if (!minerInRound.IsMinedBlockForCurrentRound())
            {
                var behaviour =
                    GetBehaviourIfMinerDidNotMineBlockForCurrentRound(currentRound, publicKey,
                        isFirstRoundOfCurrentTerm);

                if (!isTimeSlotPassed && behaviour == AElfConsensusBehaviour.Nothing)
                {
                    Context.LogDebug(() => "Directly go to next round.");
                    behaviour = AElfConsensusBehaviour.UpdateValue;
                }

                if (behaviour != AElfConsensusBehaviour.Nothing)
                {
                    return behaviour;
                }
            }
            else if (!isTimeSlotPassed)
            {
                if (minerInRound.ProducedTinyBlocks < AEDPoSContractConstants.TinyBlocksNumber)
                {
                    return AElfConsensusBehaviour.TinyBlock;
                }

                if (currentRound.ExtraBlockProducerOfPreviousRound == publicKey &&
                    !isFirstRoundOfCurrentTerm &&
                    minerInRound.ProducedTinyBlocks < AEDPoSContractConstants.TinyBlocksNumber.Mul(2))
                {
                    return AElfConsensusBehaviour.TinyBlock;
                }
            }

            // Side chain will go next round directly.
            return State.TimeEachTerm.Value == int.MaxValue
                ? AElfConsensusBehaviour.NextRound
                : GetBehaviourForChainAbleToChangeTerm(currentRound, termNumber);
        }

        /// <summary>
        /// Get consensus behaviour if miner didn't mine block for current round.
        /// </summary>
        /// <param name="currentRound"></param>
        /// <param name="publicKey"></param>
        /// <param name="isFirstRoundOfCurrentTerm"></param>
        /// <returns></returns>
        private AElfConsensusBehaviour GetBehaviourIfMinerDidNotMineBlockForCurrentRound(Round currentRound,
            string publicKey, bool isFirstRoundOfCurrentTerm)
        {
            var minerInRound = currentRound.RealTimeMinersInformation[publicKey];

            if (currentRound.RoundNumber == 1 && // For first round, the expected mining time is incorrect,
                minerInRound.Order != 1 && // so we'd better prevent miners' ain't first order (meanwhile isn't boot miner) from mining fork blocks
                currentRound.FirstMiner().OutValue == null // by postpone their mining time
            )
            {
                return AElfConsensusBehaviour.NextRound;
            }

            if (currentRound.ExtraBlockProducerOfPreviousRound == publicKey && // If this miner is extra block producer of previous round,
                Context.CurrentBlockTime < currentRound.GetStartTime() && // and currently the time is ahead of current round,
                minerInRound.ProducedTinyBlocks < AEDPoSContractConstants.TinyBlocksNumber // make this miner produce some tiny blocks.
            )
            {
                return AElfConsensusBehaviour.TinyBlock;
            }

            return isFirstRoundOfCurrentTerm
                ? AElfConsensusBehaviour.UpdateValueWithoutPreviousInValue
                : AElfConsensusBehaviour.Nothing;
        }

        /// <summary>
        /// Get consensus behaviour for chain that able to change term,
        /// like AElf Main Chain.
        /// </summary>
        /// <param name="currentRound"></param>
        /// <param name="termNumber"></param>
        /// <returns></returns>
        private AElfConsensusBehaviour GetBehaviourForChainAbleToChangeTerm(Round currentRound, long termNumber)
        {
            // In first round, the blockchain start timestamp is incorrect.
            // We can return NextRound directly.
            if (currentRound.RoundNumber == 1)
            {
                return AElfConsensusBehaviour.NextRound;
            }

            if (!TryToGetPreviousRoundInformation(out var previousRound))
            {
                Assert(false, $"Failed to get previous round information at height {Context.CurrentHeight}");
            }

            Assert(TryToGetBlockchainStartTimestamp(out var blockchainStartTimestamp),
                "Failed to get blockchain start timestamp.");

            Context.LogDebug(() => $"Using start timestamp: {blockchainStartTimestamp}");

            // Calculate the approvals and make the judgement of changing term.
            return currentRound.IsTimeToChangeTerm(previousRound, blockchainStartTimestamp, termNumber,
                State.TimeEachTerm.Value)
                ? AElfConsensusBehaviour.NextTerm
                : AElfConsensusBehaviour.NextRound;
        }
        
        private bool IsInCurrentMinerList(Round currentRound, string publicKey)
        {
            return currentRound.RealTimeMinersInformation.ContainsKey(publicKey);
        }
    }
}