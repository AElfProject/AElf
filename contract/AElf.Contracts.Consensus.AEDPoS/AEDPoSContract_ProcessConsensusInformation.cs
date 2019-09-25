using System.Linq;
using System.Runtime.CompilerServices;
using AElf.Contracts.Election;
using AElf.Contracts.Treasury;
using AElf.Sdk.CSharp;
using AElf.Types;

namespace AElf.Contracts.Consensus.AEDPoS
{
    // ReSharper disable once InconsistentNaming
    public partial class AEDPoSContract
    {
        private void ProcessConsensusInformation(dynamic input, [CallerMemberName] string caller = null)
        {
            Context.LogDebug(() => $"Processing {caller}");
            /* Privilege check. */
            if (!PreCheck())
            {
                return;
            }

            var behaviour = AElfConsensusBehaviour.Nothing;

            switch (input)
            {
                case Round round when caller == nameof(NextRound):
                    ProcessNextRound(round);
                    behaviour = AElfConsensusBehaviour.NextRound;
                    break;
                case Round round when caller == nameof(NextTerm):
                    ProcessNextTerm(round);
                    behaviour = AElfConsensusBehaviour.NextTerm;
                    break;
                case UpdateValueInput updateValueInput:
                    ProcessUpdateValue(updateValueInput);
                    behaviour = AElfConsensusBehaviour.UpdateValue;
                    break;
                case TinyBlockInput tinyBlockInput:
                    ProcessTinyBlock(tinyBlockInput);
                    behaviour = AElfConsensusBehaviour.TinyBlock;
                    break;
            }

            var miningInformationUpdated = new MiningInformationUpdated
            {
                Pubkey = _processingBlockMinerPubkey,
                Behaviour = behaviour,
                MiningTime = Context.CurrentBlockTime,
                BlockHeight = Context.CurrentHeight,
                PreviousBlockHash = Context.PreviousBlockHash
            };
            Context.Fire(miningInformationUpdated);
            Context.LogDebug(() => miningInformationUpdated.ToString());

            ResetLatestProviderToTinyBlocksCount();

            ClearCachedFields();
        }

        private void ProcessNextRound(Round nextRound)
        {
            RecordMinedMinerListOfCurrentRound();

            TryToGetCurrentRoundInformation(out var currentRound, true);

            // Do some other stuff during the first time to change round.
            if (currentRound.RoundNumber == 1)
            {
                // Set blockchain start timestamp.
                var actualBlockchainStartTimestamp = currentRound.FirstActualMiner()?.ActualMiningTimes.FirstOrDefault() ??
                                                     Context.CurrentBlockTime;
                SetBlockchainStartTimestamp(actualBlockchainStartTimestamp);
                //currentRound.RealTimeMinersInformation.First().Value.ActualMiningTimes.First();

                // Initialize current miners' information in Election Contract.
                if (State.IsMainChain.Value)
                {
                    var minersCount = GetMinersCount(nextRound);
                    if (minersCount != 0 && State.ElectionContract.Value != null)
                    {
                        State.ElectionContract.UpdateMinersCount.Send(new UpdateMinersCountInput
                        {
                            MinersCount = minersCount
                        });
                    }
                }
            }

            if (currentRound.TryToDetectEvilMiners(out var evilMiners))
            {
                Context.LogDebug(() => "Evil miners detected.");
                foreach (var evilMiner in evilMiners)
                {
                    // Mark these evil miners.
                    State.ElectionContract.UpdateCandidateInformation.Send(new UpdateCandidateInformationInput
                    {
                        Pubkey = evilMiner,
                        IsEvilNode = true
                    });
                }
            }

            Assert(TryToAddRoundInformation(nextRound), "Failed to add round information.");
            
            Assert(TryToUpdateRoundNumber(nextRound.RoundNumber), "Failed to update round number.");

            ClearExpiredRandomNumberTokens();
        }

        private void ProcessNextTerm(Round nextRound)
        {
            RecordMinedMinerListOfCurrentRound();

            // Count missed time slot of current round.
            CountMissedTimeSlots();

            Assert(TryToGetTermNumber(out var termNumber), "Term number not found.");

            // Update current term number and current round number.
            Assert(TryToUpdateTermNumber(nextRound.TermNumber), "Failed to update term number.");
            Assert(TryToUpdateRoundNumber(nextRound.RoundNumber), "Failed to update round number.");

            UpdateMinersCountToElectionContract(nextRound);

            // Reset some fields of first two rounds of next term.
            foreach (var minerInRound in nextRound.RealTimeMinersInformation.Values)
            {
                minerInRound.MissedTimeSlots = 0;
                minerInRound.ProducedBlocks = 0;
            }

            UpdateProducedBlocksNumberOfSender(nextRound);

            // Update miners list.
            var miners = new MinerList();
            miners.Pubkeys.AddRange(nextRound.RealTimeMinersInformation.Keys.Select(k => k.ToByteString()));
            if (!SetMinerList(miners, nextRound.TermNumber))
            {
                Assert(false, "Failed to update miner list.");
            }

            // Update term number lookup. (Using term number to get first round number of related term.)
            State.FirstRoundNumberOfEachTerm[nextRound.TermNumber] = nextRound.RoundNumber;

            // Update rounds information of next two rounds.
            Assert(TryToAddRoundInformation(nextRound), "Failed to add round information.");

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

            Context.LogDebug(() => $"Changing term number to {nextRound.TermNumber}");
        }

        private void RecordMinedMinerListOfCurrentRound()
        {
            TryToGetCurrentRoundInformation(out var currentRound);

            State.MinedMinerListMap.Set(currentRound.RoundNumber, new MinerList
            {
                Pubkeys = {currentRound.GetMinedMiners().Select(m => m.Pubkey.ToByteString())}
            });
        }

        private void ProcessUpdateValue(UpdateValueInput updateValueInput)
        {
            TryToGetCurrentRoundInformation(out var currentRound, true);

            var minerInRound = currentRound.RealTimeMinersInformation[_processingBlockMinerPubkey];
            minerInRound.ActualMiningTimes.Add(updateValueInput.ActualMiningTime);
            minerInRound.ProducedBlocks = updateValueInput.ProducedBlocks;
            minerInRound.ProducedTinyBlocks =
                currentRound.RealTimeMinersInformation[_processingBlockMinerPubkey].ProducedTinyBlocks.Add(1);
            minerInRound.Signature = updateValueInput.Signature;
            minerInRound.OutValue = updateValueInput.OutValue;
            minerInRound.SupposedOrderOfNextRound = updateValueInput.SupposedOrderOfNextRound;
            minerInRound.FinalOrderOfNextRound = updateValueInput.SupposedOrderOfNextRound;
            minerInRound.ImpliedIrreversibleBlockHeight = updateValueInput.ImpliedIrreversibleBlockHeight;

            PerformSecretSharing(updateValueInput, minerInRound, currentRound, _processingBlockMinerPubkey);

            UpdatePreviousInValues(updateValueInput, _processingBlockMinerPubkey, currentRound);

            foreach (var tuneOrder in updateValueInput.TuneOrderInformation)
            {
                currentRound.RealTimeMinersInformation[tuneOrder.Key].FinalOrderOfNextRound = tuneOrder.Value;
            }

            // It is permissible for miners not publish their in values.
            if (updateValueInput.PreviousInValue != Hash.Empty)
            {
                minerInRound.PreviousInValue = updateValueInput.PreviousInValue;
            }

            if (TryToGetPreviousRoundInformation(out var previousRound))
            {
                new LastIrreversibleBlockHeightCalculator(currentRound, previousRound).Deconstruct(
                    out var libHeight);
                Context.LogDebug(() => $"Finished calculation of lib height: {libHeight}");
                // LIB height can't be available if it is lower than last time.
                if (currentRound.ConfirmedIrreversibleBlockHeight < libHeight)
                {
                    Context.LogDebug(() => $"New lib height: {libHeight}");
                    Context.Fire(new IrreversibleBlockFound
                    {
                        IrreversibleBlockHeight = libHeight
                    });
                    currentRound.ConfirmedIrreversibleBlockHeight = libHeight;
                    currentRound.ConfirmedIrreversibleBlockRoundNumber = currentRound.RoundNumber.Sub(1);
                }
            }

            if (!TryToUpdateRoundInformation(currentRound))
            {
                Assert(false, "Failed to update round information.");
            }
        }

        private void ProcessTinyBlock(TinyBlockInput tinyBlockInput)
        {
            TryToGetCurrentRoundInformation(out var currentRound, true);

            currentRound.RealTimeMinersInformation[_processingBlockMinerPubkey].ActualMiningTimes
                .Add(tinyBlockInput.ActualMiningTime);
            currentRound.RealTimeMinersInformation[_processingBlockMinerPubkey].ProducedBlocks =
                tinyBlockInput.ProducedBlocks;
            var producedTinyBlocks =
                currentRound.RealTimeMinersInformation[_processingBlockMinerPubkey].ProducedTinyBlocks;
            currentRound.RealTimeMinersInformation[_processingBlockMinerPubkey].ProducedTinyBlocks =
                producedTinyBlocks.Add(1);

            Assert(TryToUpdateRoundInformation(currentRound), "Failed to update round information.");
        }

        /// <summary>
        /// The transaction can still executed successfully if the pre-check failed,
        /// though doing nothing about updating state.
        /// </summary>
        /// <returns></returns>
        private bool PreCheck()
        {
            TryToGetCurrentRoundInformation(out var currentRound);
            TryToGetPreviousRoundInformation(out var previousRound);

            _processingBlockMinerPubkey = Context.RecoverPublicKey().ToHex();

            // Though we've already prevented related transactions from inserting to the transaction pool
            // via ConstrainedAEDPoSTransactionValidationProvider,
            // this kind of permission check is still useful.
            if (!currentRound.IsInMinerList(_processingBlockMinerPubkey) &&
                !previousRound.IsInMinerList(_processingBlockMinerPubkey)) // Case a failed miner performing NextTerm
            {
                return false;
            }

            return true;
        }

        private void ResetLatestProviderToTinyBlocksCount()
        {
            LatestProviderToTinyBlocksCount currentValue;
            if (State.LatestProviderToTinyBlocksCount.Value == null)
            {
                currentValue = new LatestProviderToTinyBlocksCount
                {
                    Pubkey = _processingBlockMinerPubkey,
                    BlocksCount = AEDPoSContractConstants.MaximumTinyBlocksCount.Sub(1)
                };
                State.LatestProviderToTinyBlocksCount.Value = currentValue;
            }
            else
            {
                currentValue = State.LatestProviderToTinyBlocksCount.Value;
                if (currentValue.Pubkey == _processingBlockMinerPubkey)
                {
                    State.LatestProviderToTinyBlocksCount.Value = new LatestProviderToTinyBlocksCount
                    {
                        Pubkey = _processingBlockMinerPubkey,
                        BlocksCount = currentValue.BlocksCount.Sub(1)
                    };
                }
                else
                {
                    State.LatestProviderToTinyBlocksCount.Value = new LatestProviderToTinyBlocksCount
                    {
                        Pubkey = _processingBlockMinerPubkey,
                        BlocksCount = GetMaximumBlocksCount().Sub(1)
                    };
                }
            }
        }

        private void ClearCachedFields()
        {
            _rounds.Clear();
            _processingBlockMinerPubkey = null;
        }
    }
}