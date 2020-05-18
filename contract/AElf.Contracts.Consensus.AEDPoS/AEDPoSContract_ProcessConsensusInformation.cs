using System.Linq;
using System.Runtime.CompilerServices;
using Acs10;
using AElf.Contracts.Election;
using AElf.Contracts.TokenHolder;
using AElf.Contracts.Treasury;
using AElf.CSharp.Core;
using AElf.Sdk.CSharp;
using AElf.Types;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Contracts.Consensus.AEDPoS
{
    // ReSharper disable once InconsistentNaming
    public partial class AEDPoSContract
    {
        /// <summary>
        /// Same process for every behaviour.
        /// </summary>
        /// <param name="input"></param>
        /// <param name="callerMethodName"></param>
        private void ProcessConsensusInformation(dynamic input, [CallerMemberName] string callerMethodName = null)
        {
            EnsureTransactionOnlyExecutedOnceInOneBlock();

            Context.LogDebug(() => $"Processing {callerMethodName}");

            /* Privilege check. */
            if (!PreCheck())
            {
                return;
            }

            State.RoundBeforeLatestExecution.Value = GetCurrentRoundInformation(new Empty());

            // The only difference.
            switch (input)
            {
                case Round round when callerMethodName == nameof(NextRound):
                    ProcessNextRound(round);
                    break;
                case Round round when callerMethodName == nameof(NextTerm):
                    ProcessNextTerm(round);
                    break;
                case UpdateValueInput updateValueInput:
                    ProcessUpdateValue(updateValueInput);
                    break;
                case TinyBlockInput tinyBlockInput:
                    ProcessTinyBlock(tinyBlockInput);
                    break;
            }

            var miningInformationUpdated = new MiningInformationUpdated
            {
                // _processingBlockMinerPubkey is set during PreCheck.
                Pubkey = _processingBlockMinerPubkey,
                Behaviour = callerMethodName,
                MiningTime = Context.CurrentBlockTime,
                BlockHeight = Context.CurrentHeight,
                PreviousBlockHash = Context.PreviousBlockHash
            };
            Context.LogDebug(() => $"Synced mining information: {miningInformationUpdated}");

            // Make sure the method GetMaximumBlocksCount executed no matter what consensus behaviour is.
            var minersCountInTheory = GetMaximumBlocksCount();
            ResetLatestProviderToTinyBlocksCount(minersCountInTheory);

            if (TryToGetCurrentRoundInformation(out var currentRound))
            {
                Context.LogDebug(() =>
                    $"Current round information:\n{currentRound.ToString(_processingBlockMinerPubkey)}");
            }

            var latestSignature = GetLatestSignature(currentRound);
            var previousRandomHash = State.RandomHashes[Context.CurrentHeight.Sub(1)];
            var randomHash = previousRandomHash == null
                ? latestSignature
                : HashHelper.XorAndCompute(previousRandomHash, latestSignature);

            State.RandomHashes[Context.CurrentHeight] = randomHash;

            Context.LogDebug(() => $"New random hash generated: {randomHash} - height {Context.CurrentHeight}");

            if (!State.IsMainChain.Value && currentRound.RoundNumber > 1)
            {
                Release();
            }

            // Clear cache.
            _processingBlockMinerPubkey = null;
        }

        /// <summary>
        /// Get latest updated signature.
        /// A signature is for generating a random hash.
        /// </summary>
        /// <param name="currentRound"></param>
        /// <returns></returns>
        private Hash GetLatestSignature(Round currentRound)
        {
            var latestSignature = currentRound.RealTimeMinersInformation.Values.OrderBy(m => m.Order)
                .LastOrDefault(m => m.Signature != null)?.Signature;
            if (latestSignature != null) return latestSignature;
            if (TryToGetPreviousRoundInformation(out var previousRound))
            {
                latestSignature = previousRound.RealTimeMinersInformation.Values.OrderBy(m => m.Order)
                    .LastOrDefault(m => m.Signature != null)
                    ?.Signature;
            }

            return latestSignature;
        }

        private void ProcessNextRound(Round nextRound)
        {
            RecordMinedMinerListOfCurrentRound();

            TryToGetCurrentRoundInformation(out var currentRound);

            // Do some other stuff during the first time to change round.
            if (currentRound.RoundNumber == 1)
            {
                // Set blockchain start timestamp.
                var actualBlockchainStartTimestamp =
                    currentRound.FirstActualMiner()?.ActualMiningTimes.FirstOrDefault() ??
                    Context.CurrentBlockTime;
                SetBlockchainStartTimestamp(actualBlockchainStartTimestamp);

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

            if (State.IsMainChain.Value && // Only detect evil miners in Main Chain.
                currentRound.TryToDetectEvilMiners(out var evilMiners))
            {
                Context.LogDebug(() => "Evil miners detected.");
                foreach (var evilMiner in evilMiners)
                {
                    Context.LogDebug(() =>
                        $"Evil miner {evilMiner}, missed time slots: {currentRound.RealTimeMinersInformation[evilMiner].MissedTimeSlots}.");
                    // Mark these evil miners.
                    State.ElectionContract.UpdateCandidateInformation.Send(new UpdateCandidateInformationInput
                    {
                        Pubkey = evilMiner,
                        IsEvilNode = true
                    });
                }
            }

            AddRoundInformation(nextRound);

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
            AddRoundInformation(nextRound);

            if (!TryToGetPreviousRoundInformation(out var previousRound))
            {
                Assert(false, "Failed to get previous round information.");
            }

            UpdateCurrentMinerInformationToElectionContract(previousRound);

            if (DonateMiningReward(previousRound))
            {
                State.TreasuryContract.Release.Send(new ReleaseInput
                {
                    PeriodNumber = termNumber
                });

                Context.LogDebug(() => $"Released treasury profit for term {termNumber}");
            }

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
            TryToGetCurrentRoundInformation(out var currentRound);

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

        private static void PerformSecretSharing(UpdateValueInput input, MinerInRound minerInRound, Round round,
            string publicKey)
        {
            minerInRound.EncryptedPieces.Add(input.EncryptedPieces);
            foreach (var decryptedPreviousInValue in input.DecryptedPieces)
            {
                round.RealTimeMinersInformation[decryptedPreviousInValue.Key].DecryptedPieces
                    .Add(publicKey, decryptedPreviousInValue.Value);
            }

            foreach (var previousInValue in input.MinersPreviousInValues)
            {
                round.RealTimeMinersInformation[previousInValue.Key].PreviousInValue = previousInValue.Value;
            }
        }

        private void ProcessTinyBlock(TinyBlockInput tinyBlockInput)
        {
            TryToGetCurrentRoundInformation(out var currentRound);

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

        /// <summary>
        /// To prevent one miner produced too many continuous blocks.
        /// </summary>
        /// <param name="minersCountInTheory"></param>
        private void ResetLatestProviderToTinyBlocksCount(int minersCountInTheory)
        {
            LatestPubkeyToTinyBlocksCount currentValue;
            if (State.LatestPubkeyToTinyBlocksCount.Value == null)
            {
                currentValue = new LatestPubkeyToTinyBlocksCount
                {
                    Pubkey = _processingBlockMinerPubkey,
                    BlocksCount = AEDPoSContractConstants.MaximumTinyBlocksCount.Sub(1)
                };
                State.LatestPubkeyToTinyBlocksCount.Value = currentValue;
            }
            else
            {
                currentValue = State.LatestPubkeyToTinyBlocksCount.Value;
                if (currentValue.Pubkey == _processingBlockMinerPubkey)
                {
                    State.LatestPubkeyToTinyBlocksCount.Value = new LatestPubkeyToTinyBlocksCount
                    {
                        Pubkey = _processingBlockMinerPubkey,
                        BlocksCount = currentValue.BlocksCount.Sub(1)
                    };
                }
                else
                {
                    State.LatestPubkeyToTinyBlocksCount.Value = new LatestPubkeyToTinyBlocksCount
                    {
                        Pubkey = _processingBlockMinerPubkey,
                        BlocksCount = minersCountInTheory.Sub(1)
                    };
                }
            }
        }
    }
}