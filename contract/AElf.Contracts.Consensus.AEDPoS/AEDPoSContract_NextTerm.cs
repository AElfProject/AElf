using System.Linq;
using AElf.Contracts.Election;
using AElf.Sdk.CSharp;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Contracts.Consensus.AEDPoS
{
    public partial class AEDPoSContract
    {
        public override Empty NextTerm(Round input)
        {
            // Count missed time slot of current round.
            CountMissedTimeSlots();

            Assert(TryToGetTermNumber(out var termNumber), "Term number not found.");

            // Update current term number and current round number.
            Assert(TryToUpdateTermNumber(input.TermNumber), "Failed to update term number.");
            Assert(TryToUpdateRoundNumber(input.RoundNumber), "Failed to update round number.");

            var minersCount = GetMinersCount(input);
            if (minersCount != 0 && State.ElectionContract.Value != null)
            {
                State.ElectionContract.UpdateMinersCount.Send(new UpdateMinersCountInput
                {
                    MinersCount = minersCount
                });
            }
            // Reset some fields of first two rounds of next term.
            foreach (var minerInRound in input.RealTimeMinersInformation.Values)
            {
                minerInRound.MissedTimeSlots = 0;
                minerInRound.ProducedBlocks = 0;
            }

            var senderPublicKey = Context.RecoverPublicKey().ToHex();

            // Update produced block number of this node.
            if (input.RealTimeMinersInformation.ContainsKey(senderPublicKey))
            {
                input.RealTimeMinersInformation[senderPublicKey].ProducedBlocks =
                    input.RealTimeMinersInformation[senderPublicKey].ProducedBlocks + 1;
            }
            else
            {
                State.ElectionContract.UpdateCandidateInformation.Send(new UpdateCandidateInformationInput
                {
                    PublicKey = senderPublicKey,
                    RecentlyProducedBlocks = 1
                });
            }

            // Update miners list.
            var miners = new MinerList();
            miners.Pubkeys.AddRange(input.RealTimeMinersInformation.Keys.Select(k => k.ToByteString()));
            Assert(SetMinerListOfCurrentTerm(miners), "Failed to update miner list.");

            // Update term number lookup. (Using term number to get first round number of related term.)
            State.FirstRoundNumberOfEachTerm[input.TermNumber] = input.RoundNumber;

            // Update rounds information of next two rounds.
            Assert(TryToAddRoundInformation(input), "Failed to add round information.");

            Assert(TryToGetPreviousRoundInformation(out var previousRound),
                "Failed to get previous round information.");

            foreach (var minerInfo in previousRound.RealTimeMinersInformation)
            {
                State.ElectionContract.UpdateCandidateInformation.Send(new UpdateCandidateInformationInput
                {
                    PublicKey = minerInfo.Key,
                    RecentlyProducedBlocks = minerInfo.Value.ProducedBlocks,
                    RecentlyMissedTimeSlots = minerInfo.Value.MissedTimeSlots
                });
            }

            State.ElectionContract.ReleaseTreasuryProfits.Send(new ReleaseTreasuryProfitsInput
            {
                MinedBlocks = previousRound.GetMinedBlocks(),
                TermNumber = termNumber + 1,
                RoundNumber = previousRound.RoundNumber
            });

            Context.LogDebug(() => $"Changing term number to {input.TermNumber}");

            return new Empty();
        }

        private bool SetMinerListOfCurrentTerm(MinerList minerList, bool gonnaReplaceSomeone = false)
        {
            // Miners for one specific term should only update once.
            var termNumber = State.CurrentTermNumber.Value;
            var minerListFromState = State.MinerListMap[termNumber];
            if (gonnaReplaceSomeone || minerListFromState == null)
            {
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