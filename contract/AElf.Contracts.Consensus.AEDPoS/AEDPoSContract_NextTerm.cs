using System.Linq;
using AElf.Contracts.Election;
using AElf.Contracts.MultiToken.Messages;
using AElf.Contracts.Treasury;
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

            UpdateMinersCountToElectionContract(input);

            // Reset some fields of first two rounds of next term.
            foreach (var minerInRound in input.RealTimeMinersInformation.Values)
            {
                minerInRound.MissedTimeSlots = 0;
                minerInRound.ProducedBlocks = 0;
            }

            UpdateProducedBlocksNumberOfSender(input);

            // Update miners list.
            var miners = new MinerList();
            miners.Pubkeys.AddRange(input.RealTimeMinersInformation.Keys.Select(k => k.ToByteString()));
            if (!SetMinerList(miners, input.TermNumber))
            {
                Assert(false, "Failed to update miner list.");
            }

            // Update term number lookup. (Using term number to get first round number of related term.)
            State.FirstRoundNumberOfEachTerm[input.TermNumber] = input.RoundNumber;

            // Update rounds information of next two rounds.
            Assert(TryToAddRoundInformation(input), "Failed to add round information.");

            if (!TryToGetPreviousRoundInformation(out var previousRound))
            {
                Assert(false, "Failed to get previous round information.");
            }

            UpdateCurrentMinerInformationToElectionContract(previousRound);

            DonateMiningReward(previousRound);

            State.TreasuryContract.Release.Send(new ReleaseInput
            {
                TermNumber = termNumber
            });

            Context.LogDebug(() => $"Released treasury profit for term {termNumber}");

            State.ElectionContract.TakeSnapshot.Send(new TakeElectionSnapshotInput
            {
                MinedBlocks = previousRound.GetMinedBlocks(),
                TermNumber = termNumber,
                RoundNumber = previousRound.RoundNumber
            });

            Context.LogDebug(() => $"Changing term number to {input.TermNumber}");

            return new Empty();
        }

        private void UpdateProducedBlocksNumberOfSender(Round input)
        {
            var senderPublicKey = Context.RecoverPublicKey().ToHex();

            // Update produced block number of transaction sender.
            if (input.RealTimeMinersInformation.ContainsKey(senderPublicKey))
            {
                input.RealTimeMinersInformation[senderPublicKey].ProducedBlocks =
                    input.RealTimeMinersInformation[senderPublicKey].ProducedBlocks + 1;
            }
            else
            {
                // If the sender isn't in miner list of next term.
                State.ElectionContract.UpdateCandidateInformation.Send(new UpdateCandidateInformationInput
                {
                    Pubkey = senderPublicKey,
                    RecentlyProducedBlocks = 1
                });
            }
        }

        private void UpdateCurrentMinerInformationToElectionContract(Round previousRound)
        {
            State.ElectionContract.UpdateMultipleCandidateInformation.Send(new UpdateMultipleCandidateInformationInput
            {
                Value =
                {
                    previousRound.RealTimeMinersInformation.Select(i => new UpdateCandidateInformationInput
                    {
                        Pubkey = i.Key,
                        RecentlyProducedBlocks = i.Value.ProducedBlocks,
                        RecentlyMissedTimeSlots = i.Value.MissedTimeSlots
                    })
                }
            });
        }

        private void UpdateMinersCountToElectionContract(Round input)
        {
            var minersCount = GetMinersCount(input);
            if (minersCount != 0 && State.ElectionContract.Value != null)
            {
                State.ElectionContract.UpdateMinersCount.Send(new UpdateMinersCountInput
                {
                    MinersCount = minersCount
                });
            }
        }

        private bool SetMinerList(MinerList minerList, long termNumber, bool gonnaReplaceSomeone = false)
        {
            // Miners for one specific term should only update once.
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

        private void DonateMiningReward(Round previousRound)
        {
            if (State.TreasuryContract.Value == null)
            {
                State.TreasuryContract.Value =
                    Context.GetContractAddressByName(SmartContractConstants.TreasuryContractSystemName);
            }
            
            var amount = previousRound.GetMinedBlocks().Mul(AEDPoSContractConstants.MiningRewardPerBlock);

            if (amount > 0)
            {
                State.TreasuryContract.Donate.Send(new DonateInput
                {
                    Symbol = Context.Variables.NativeSymbol,
                    Amount = amount,
                });
            }

            Context.LogDebug(() => $"Released {amount} mining rewards.");
        }
    }
}