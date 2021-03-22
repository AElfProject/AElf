using AElf.CSharp.Core;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Contracts.Consensus.AEDPoS
{
    // ReSharper disable once InconsistentNaming
    public partial class AEDPoSContract
    {
        public override Empty NextTerm(Round input)
        {
            SupplyCurrentRoundInformation();
            ProcessConsensusInformation(input);
            return new Empty();
        }

        private void UpdateProducedBlocksNumberOfSender(Round input)
        {
            var senderPubkey = Context.RecoverPublicKey().ToHex();

            // Update produced block number of transaction sender.
            if (input.RealTimeMinersInformation.ContainsKey(senderPubkey))
            {
                input.RealTimeMinersInformation[senderPubkey].ProducedBlocks =
                    input.RealTimeMinersInformation[senderPubkey].ProducedBlocks.Add(1);
            }
        }

        /// <summary>
        /// Only Main Chain can perform this action.
        /// </summary>
        /// <param name="minerList"></param>
        /// <param name="termNumber"></param>
        /// <param name="gonnaReplaceSomeone"></param>
        /// <returns></returns>
        private bool SetMinerList(MinerList minerList, long termNumber, bool gonnaReplaceSomeone = false)
        {
            // Miners for one specific term should only update once.
            var minerListFromState = State.MinerListMap[termNumber];
            if (gonnaReplaceSomeone || minerListFromState == null)
            {
                State.MainChainCurrentMinerList.Value = minerList;
                State.MinerListMap[termNumber] = minerList;
                return true;
            }

            return false;
        }

        /// <summary>
        /// Normally this process contained in NextRound method.
        /// </summary>
        private void CountMissedTimeSlots()
        {
            if (!TryToGetCurrentRoundInformation(out var currentRound)) return;

            foreach (var minerInRound in currentRound.RealTimeMinersInformation)
            {
                if (minerInRound.Value.OutValue == null)
                {
                    minerInRound.Value.MissedTimeSlots = minerInRound.Value.MissedTimeSlots.Add(1);
                }
            }

            TryToUpdateRoundInformation(currentRound);
        }

        private bool TryToUpdateTermNumber(long termNumber)
        {
            var oldTermNumber = State.CurrentTermNumber.Value;
            if (termNumber != 1 && oldTermNumber + 1 != termNumber)
            {
                return false;
            }

            State.CurrentTermNumber.Value = termNumber;
            return true;
        }
    }
}