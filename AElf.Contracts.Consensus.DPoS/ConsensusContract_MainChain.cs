using System;
using System.Collections.Generic;
using System.Linq;
using AElf.Common;
using AElf.Kernel;
using AElf.Sdk.CSharp;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Contracts.Consensus.DPoS
{
    public partial class ConsensusContract : IMainChainDPoSConsensusSmartContract
    {
        public void Initialize(Address tokenContractAddress, Address dividendsContractAddress)
        {
            Assert(!State.Initialized.Value, "Already initialized.");
            State.TokenContract.Value = tokenContractAddress;
            State.DividendContract.Value = dividendsContractAddress;
            State.Initialized.Value = true;
        }

        private bool GenerateNextRoundInformation(Round currentRound, Timestamp timestamp,
            Timestamp blockchainStartTimestamp, out Round nextRound)
        {
            return currentRound.GenerateNextRoundInformation(timestamp, blockchainStartTimestamp, out nextRound);
        }

        private void InitialSettings(Round firstRound)
        {
            // Do some initializations.
            SetTermNumber(1);
            SetRoundNumber(1);
            SetBlockAge(1);
            AddTermNumberToFirstRoundNumber(1, 1);
            SetBlockchainStartTimestamp(firstRound.GetStartTime());
            var miners = firstRound.RealTimeMinersInformation.Keys.ToList().ToMiners(1);
            miners.TermNumber = 1;
            SetMiners(miners);
            SetMiningInterval(firstRound.GetMiningInterval());
            
            var senderPublicKey = Context.RecoverPublicKey().ToHex();

            // Create a new history information.
            var historyInformation = new CandidateInHistory
            {
                PublicKey = senderPublicKey,
                ProducedBlocks = 1,
                CurrentAlias = senderPublicKey.Substring(0, DPoSContractConsts.AliasLimit)
            };

            AddOrUpdateMinerHistoryInformation(historyInformation);
        }

        private void UpdateHistoryInformation(Round round)
        {
            var senderPublicKey = Context.RecoverPublicKey().ToHex();

            Assert(TryToGetCurrentRoundInformation(out var currentRound), "Failed to get current round information.");

            // Update missed time slots and produced blocks for each miner.
            foreach (var minerInRound in currentRound.RealTimeMinersInformation)
            {
                if (round.RealTimeMinersInformation.ContainsKey(minerInRound.Key))
                {
                    round.RealTimeMinersInformation[minerInRound.Key].MissedTimeSlots =
                        minerInRound.Value.MissedTimeSlots;
                    round.RealTimeMinersInformation[minerInRound.Key].ProducedBlocks =
                        minerInRound.Value.ProducedBlocks;
                }
                else
                {
                    if (TryToGetMinerHistoryInformation(senderPublicKey, out var historyInformation))
                    {
                        historyInformation.ProducedBlocks += minerInRound.Value.ProducedBlocks;
                        historyInformation.MissedTimeSlots += minerInRound.Value.MissedTimeSlots;
                    }
                    else
                    {
                        historyInformation = new CandidateInHistory
                        {
                            PublicKey = senderPublicKey,
                            ProducedBlocks = minerInRound.Value.ProducedBlocks,
                            MissedTimeSlots = minerInRound.Value.MissedTimeSlots,
                            CurrentAlias = senderPublicKey.Substring(0, DPoSContractConsts.AliasLimit)
                        };
                    }

                    AddOrUpdateMinerHistoryInformation(historyInformation);
                }
            }

            if (round.RealTimeMinersInformation.ContainsKey(senderPublicKey))
            {
                round.RealTimeMinersInformation[senderPublicKey].ProducedBlocks += 1;
            }
            else
            {
                if (TryToGetMinerHistoryInformation(senderPublicKey, out var historyInformation))
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
                }

                AddOrUpdateMinerHistoryInformation(historyInformation);
            }
        }
        
        public void NextTerm(Round round)
        {
            // Count missed time slot of current round.
            CountMissedTimeSlots();

            Assert(TryToGetTermNumber(out var termNumber), "Term number not found.");
            State.DividendContract.KeepWeights(termNumber);

            // Update current term number and current round number.
            Assert(TryToUpdateTermNumber(round.TermNumber), "Failed to update term number.");
            Assert(TryToUpdateRoundNumber(round.RoundNumber), "Failed to update round number.");

            // Reset some fields of first two rounds of next term.
            foreach (var minerInRound in round.RealTimeMinersInformation.Values)
            {
                minerInRound.MissedTimeSlots = 0;
                minerInRound.ProducedBlocks = 0;
            }

            var senderPublicKey = Context.RecoverPublicKey().ToHex();

            // Update produced block number of this node.
            if (round.RealTimeMinersInformation.ContainsKey(senderPublicKey))
            {
                round.RealTimeMinersInformation[senderPublicKey].ProducedBlocks += 1;
            }
            else
            {
                if (TryToGetMinerHistoryInformation(senderPublicKey, out var historyInformation))
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
                }

                AddOrUpdateMinerHistoryInformation(historyInformation);
            }

            // Update miners list.
            SetMiners(round.RealTimeMinersInformation.Keys.ToMiners(round.TermNumber));

            // Update term number lookup. (Using term number to get first round number of related term.)
            AddTermNumberToFirstRoundNumber(round.TermNumber, round.RoundNumber);

            Assert(TryToGetCurrentAge(out var blockAge), "Block age not found.");
            // Update blockchain age of next two rounds.
            round.BlockchainAge = blockAge;

            // Update rounds information of next two rounds.
            Assert(TryToAddRoundInformation(round), "Failed to add round information.");

            TryToFindLIB();
        }

        /// <summary>
        /// Take a snapshot of specific term.
        /// Basically this snapshot is used for getting ranks of candidates of specific term.
        /// </summary>
        /// <param name="snapshotTermNumber"></param>
        /// <param name="lastRoundNumber"></param>
        /// <returns></returns>
        public ActionResult SnapshotForTerm(ulong snapshotTermNumber, ulong lastRoundNumber)
        {
            if (TryToGetSnapshot(snapshotTermNumber, out _))
            {
                return new ActionResult
                {
                    Success = false,
                    ErrorMessage = $"Snapshot of term {snapshotTermNumber} already taken."
                };
            }

            if (!TryToGetRoundInformation(lastRoundNumber, out var roundInformation))
            {
                return new ActionResult
                {
                    Success = false,
                    ErrorMessage = $"Failed to get information of round {lastRoundNumber}."
                };
            }

            // To calculate the number of mined blocks.
            var minedBlocks = roundInformation.RealTimeMinersInformation.Values.Aggregate<MinerInRound, ulong>(0,
                (current, minerInRound) => current + minerInRound.ProducedBlocks);

            // Snapshot for the number of votes of new victories.
            var candidateInTerms = new List<CandidateInTerm>();
            if (TryToGetVictories(out var victories))
            {
                foreach (var candidatePublicKey in victories.PublicKeys)
                {
                    if (TryToGetTicketsInformation(candidatePublicKey, out var candidateTickets))
                    {
                        candidateInTerms.Add(new CandidateInTerm
                        {
                            PublicKey = candidatePublicKey,
                            Votes = candidateTickets.ObtainedTickets
                        });
                    }
                    else
                    {
                        AddOrUpdateTicketsInformation(new Tickets {PublicKey = candidatePublicKey});
                        candidateInTerms.Add(new CandidateInTerm
                        {
                            PublicKey = candidatePublicKey,
                            Votes = 0
                        });
                    }
                }
            }

            Assert(TryToGetRoundNumber(out var roundNumber), "Round number not found.");
            // Set snapshot of related term.
            SetSnapshot(new TermSnapshot
            {
                TermNumber = snapshotTermNumber,
                EndRoundNumber = roundNumber,
                TotalBlocks = minedBlocks,
                CandidatesSnapshot = {candidateInTerms}
            });

            Console.WriteLine($"Snapshot of term {snapshotTermNumber} taken.");

            return new ActionResult {Success = true};
        }

        public ActionResult SnapshotForMiners(ulong previousTermNumber, ulong lastRoundNumber)
        {
            Assert(TryToGetRoundInformation(lastRoundNumber, out var roundInformation),
                "Round information not found.");

            foreach (var candidate in roundInformation.RealTimeMinersInformation)
            {
                CandidateInHistory candidateInHistory;
                if (TryToGetMinerHistoryInformation(candidate.Key, out var historyInformation))
                {
                    var terms = new List<ulong>(historyInformation.Terms.ToList());

                    if (terms.Contains(previousTermNumber))
                    {
                        return new ActionResult
                            {Success = false, ErrorMessage = "Snapshot for miners in previous term already taken."};
                    }

                    terms.Add(previousTermNumber);

                    var continualAppointmentCount = historyInformation.ContinualAppointmentCount;
                    if (TryToGetMiners(previousTermNumber, out var minersOfLastTerm) &&
                        minersOfLastTerm.PublicKeys.Contains(candidate.Key))
                    {
                        continualAppointmentCount++;
                    }
                    else
                    {
                        continualAppointmentCount = 0;
                    }

                    candidateInHistory = new CandidateInHistory
                    {
                        PublicKey = candidate.Key,
                        MissedTimeSlots = historyInformation.MissedTimeSlots + candidate.Value.MissedTimeSlots,
                        ProducedBlocks = historyInformation.ProducedBlocks + candidate.Value.ProducedBlocks,
                        ContinualAppointmentCount = continualAppointmentCount,
                        ReappointmentCount = historyInformation.ReappointmentCount + 1,
                        CurrentAlias = historyInformation.CurrentAlias,
                        Terms = {terms}
                    };
                }
                else
                {
                    candidateInHistory = new CandidateInHistory
                    {
                        PublicKey = candidate.Key,
                        MissedTimeSlots = candidate.Value.MissedTimeSlots,
                        ProducedBlocks = candidate.Value.ProducedBlocks,
                        ContinualAppointmentCount = 0,
                        ReappointmentCount = 0,
                        Terms = {previousTermNumber}
                    };
                }

                AddOrUpdateMinerHistoryInformation(candidateInHistory);
            }

            return new ActionResult {Success = true};
        }

        public ActionResult SendDividends(ulong dividendsTermNumber, ulong lastRoundNumber)
        {
            Assert(TryToGetRoundInformation(lastRoundNumber, out var roundInformation),
                "Round information not found.");

            // Set dividends of related term to Dividends Contract.
            var minedBlocks = roundInformation.RealTimeMinersInformation.Values.Aggregate<MinerInRound, ulong>(0,
                (current, minerInRound) => current + minerInRound.ProducedBlocks);
            State.DividendContract.AddDividends(dividendsTermNumber, GetDividendsForVoters(minedBlocks));

            ulong totalVotes = 0;
            ulong totalReappointment = 0;
            var continualAppointmentDict = new Dictionary<string, ulong>();
            foreach (var minerInRound in roundInformation.RealTimeMinersInformation)
            {
                if (TryToGetTicketsInformation(minerInRound.Key, out var candidateTickets))
                {
                    totalVotes += candidateTickets.ObtainedTickets;
                }

                if (TryToGetMinerHistoryInformation(minerInRound.Key, out var candidateInHistory))
                {
                    totalReappointment += candidateInHistory.ContinualAppointmentCount;

                    continualAppointmentDict.Add(minerInRound.Key, candidateInHistory.ContinualAppointmentCount);
                }

                // Transfer dividends for actual miners. (The miners list based on last round of current term.)
                var amount = GetDividendsForEveryMiner(minedBlocks) +
                             (totalVotes == 0
                                 ? 0
                                 : GetDividendsForTicketsCount(minedBlocks) * candidateTickets.ObtainedTickets /
                                   totalVotes) +
                             (totalReappointment == 0
                                 ? 0
                                 : GetDividendsForReappointment(minedBlocks) *
                                   continualAppointmentDict[minerInRound.Key] /
                                   totalReappointment);
                // TODO: Can we ask the miners to claim the rewards ???
                State.DividendContract.SendDividends(
                    Address.FromPublicKey(ByteArrayHelpers.FromHexString(minerInRound.Key)), amount);
            }

            if (TryToGetBackups(roundInformation.RealTimeMinersInformation.Keys.ToList(), out var backups))
            {
                foreach (var backup in backups)
                {
                    var backupCount = (ulong) backups.Count;
                    var amount = backupCount == 0 ? 0 : GetDividendsForBackupNodes(minedBlocks) / backupCount;
                    State.DividendContract.SendDividends(Address.FromPublicKey(ByteArrayHelpers.FromHexString(backup)),
                        amount);
                }
            }

            return new ActionResult {Success = true};
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
    }
}