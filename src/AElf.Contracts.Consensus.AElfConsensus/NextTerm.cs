using System.Linq;
using AElf.Consensus.AElfConsensus;
using AElf.Kernel;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Contracts.Consensus.AElfConsensus
{
    public partial class AElfConsensusContract
    {
        public override Empty NextTerm(Round input)
        {
            // Count missed time slot of current round.
            CountMissedTimeSlots();

            Assert(TryToGetTermNumber(out var termNumber), "Term number not found.");

            // Update current term number and current round number.
            Assert(TryToUpdateTermNumber(input.TermNumber), "Failed to update term number.");
            Assert(TryToUpdateRoundNumber(input.RoundNumber), "Failed to update round number.");

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
                input.RealTimeMinersInformation[senderPublicKey].ProducedBlocks += 1;
            }
            else
            {
                // TODO: Add or update candidate history.
/*                if (TryToGetMinerHistoryInformation(senderPublicKey, out var historyInformation))
                {
                    historyInformation.ProducedBlocks += 1;
                }
                else
                {
                    historyInformation = new CandidateInHistory
                    {
                        PublicKey = senderPublicKey,
                        ProducedBlocks = 1,
                        CurrentAlias = senderPublicKey.Substring(0, DPoSContractConsts.AliasLimit)
                    };
                }*/

                //AddOrUpdateMinerHistoryInformation(historyInformation);
            }

            // Update miners list.
            var miners = new Miners {TermNumber = input.TermNumber};
            miners.PublicKeys.AddRange(input.RealTimeMinersInformation.Keys.Select(k =>
                ByteString.CopyFrom(ByteArrayHelpers.FromHexString(k))));
            Assert(SetMiners(miners), "Failed to update miners list.");

            // Update term number lookup. (Using term number to get first round number of related term.)
            State.FirstRoundNumberOfEachTerm[input.TermNumber] = input.RoundNumber;

            // Update blockchain age of new term.
            //UpdateBlockchainAge(input.BlockchainAge);

            // Update rounds information of next two rounds.
            Assert(TryToAddRoundInformation(input), "Failed to add round information.");

            State.ElectionContract.Value =
                State.BasicContractZero.GetContractAddressByName.Call(State.ElectionContractSystemName.Value);
            State.ElectionContract.ReleaseTreasuryProfits.Send(new ReleaseTreasuryProfitsInput
            {
                MinedBlocks = input.GetMinedBlocks(),
                TermNumber = termNumber
            });
                
            Context.LogDebug(() => $"Changing term number to {input.TermNumber}");
            TryToFindLIB();
            return new Empty();
        }

        private bool SetMiners(Miners miners, bool gonnaReplaceSomeone = false)
        {
            // Miners for one specific term should only update once.
            var m = State.MinersMap[miners.TermNumber];
            if (gonnaReplaceSomeone || m == null)
            {
                State.MinersMap[miners.TermNumber] = miners;
                return true;
            }

            return false;
        }
        
        /// <summary>
        /// Normally this process contained in NextRound method.
        /// </summary>
        private void CountMissedTimeSlots()
        {
            if (TryToGetCurrentRoundInformation(out var currentRound))
            {
                foreach (var minerInRound in currentRound.RealTimeMinersInformation)
                {
                    if (minerInRound.Value.OutValue == null)
                    {
                        minerInRound.Value.MissedTimeSlots += 1;
                    }
                }

                TryToUpdateRoundInformation(currentRound);
            }
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