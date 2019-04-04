using System;
using System.Collections.Generic;
using System.Linq;
using AElf.Common;
using AElf.Consensus.DPoS;
using AElf.Kernel;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Contracts.Consensus.DPoS
{
    public partial class ConsensusContract
    {
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
            Assert(SetMiners(input.RealTimeMinersInformation.Keys.ToList().ToMiners(input.TermNumber)),
                ContractErrorCode.GetErrorMessage(ContractErrorCode.AttemptFailed, "Failed to update miners list."));

            // Update term number lookup. (Using term number to get first round number of related term.)
            State.TermToFirstRoundMap[input.TermNumber.ToInt64Value()] = input.RoundNumber.ToInt64Value();

            // Update blockchain age of new term.
            UpdateBlockchainAge(input.BlockchainAge);

            // Update rounds information of next two rounds.
            Assert(TryToAddRoundInformation(input), "Failed to add round information.");

            if (State.DividendContract.Value != null)
            {
                State.DividendContract.Value =
                    State.BasicContractZero.GetContractAddressByName.Call(State.DividendContractSystemName.Value);
                State.DividendContract.KeepWeights.Send(new SInt64Value {Value = termNumber});

                var termInfo = new TermInfo
                {
                    RoundNumber = input.RoundNumber - 1,
                    TermNumber = input.TermNumber - 1
                };
                Assert(SnapshotForTerm(termInfo).Success,
                    ContractErrorCode.GetErrorMessage(ContractErrorCode.AttemptFailed,
                        $"Failed to take snapshot of term {termInfo.TermNumber}"));
                Assert(SnapshotForMiners(termInfo).Success,
                    ContractErrorCode.GetErrorMessage(ContractErrorCode.AttemptFailed,
                        $"Failed to take snapshot of miners of term {termInfo.TermNumber}"));
                Assert(SendDividends(termInfo).Success,
                    ContractErrorCode.GetErrorMessage(ContractErrorCode.AttemptFailed,
                        $"Failed to send dividends of term {termInfo.TermNumber}"));
            }

            Context.LogDebug(() => $"Changing term number to {input.TermNumber}");
            TryToFindLIB();
            return new Empty();
        }

        /// <summary>
        /// Take a snapshot of specific term.
        /// Basically this snapshot is used for getting ranks of candidates of specific term. 
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        private ActionResult SnapshotForTerm(TermInfo input)
        {
            var snapshotTermNumber = input.TermNumber;
            var lastRoundNumber = input.RoundNumber;
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
            var minedBlocks = roundInformation.RealTimeMinersInformation.Values.Aggregate<MinerInRound, long>(0,
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

            return new ActionResult {Success = true};
        }

        private ActionResult SnapshotForMiners(TermInfo input)
        {
            var lastRoundNumber = input.RoundNumber;
            var previousTermNumber = input.TermNumber;
            Assert(TryToGetRoundInformation(lastRoundNumber, out var roundInformation),
                "Round information not found.");

            foreach (var candidate in roundInformation.RealTimeMinersInformation)
            {
                CandidateInHistory candidateInHistory;
                if (TryToGetMinerHistoryInformation(candidate.Key, out var historyInformation))
                {
                    var terms = new List<long>(historyInformation.Terms.ToList());

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

        private ActionResult SendDividends(TermInfo input)
        {
            LogVerbose("Entered SendDividends");
            State.DividendContract.Value =
                State.BasicContractZero.GetContractAddressByName.Call(State.DividendContractSystemName.Value);
            LogVerbose("Set Dividend Contract State.");

            var lastRoundNumber = input.RoundNumber;
            var dividendsTermNumber = input.TermNumber;
            Assert(TryToGetRoundInformation(lastRoundNumber, out var roundInformation),
                "Round information not found.");

            LogVerbose($"Got current round information: {roundInformation}");

            // Set dividends of related term to Dividends Contract.
            var minedBlocks = roundInformation.GetMinedBlocks();
            State.DividendContract.AddDividends.Send(
                new AddDividendsInput()
                {
                    TermNumber = dividendsTermNumber,
                    DividendsAmount = GetDividendsForVoters(minedBlocks)
                });

            LogVerbose($"Added dividends of term {input.TermNumber}");

            var ticketsDict = roundInformation.RealTimeMinersInformation
                .Select(m => State.TicketsMap[m.Key.ToStringValue()] ?? new Tickets {PublicKey = m.Key})
                .ToDictionary(t => t.PublicKey, t => t);

            var historyDict = roundInformation.RealTimeMinersInformation
                .Select(m => State.HistoryMap[m.Key.ToStringValue()] ?? new CandidateInHistory {PublicKey = m.Key})
                .ToDictionary(h => h.PublicKey, h => h);

            var totalVotes = ticketsDict.Values.Sum(t => t.ObtainedTickets);
            var totalReappointment = historyDict.Values.Sum(h => h.ContinualAppointmentCount);
            
            foreach (var minerInRound in roundInformation.RealTimeMinersInformation)
            {
                // Transfer dividends for actual miners. (The miners list based on last round of old term.)
                var amount = GetDividendsForEveryMiner(minedBlocks) +
                             (totalVotes == 0
                                 ? 0
                                 : GetDividendsForTicketsCount(minedBlocks) * ticketsDict[minerInRound.Key].ObtainedTickets /
                                   totalVotes) +
                             (totalReappointment == 0
                                 ? 0
                                 : GetDividendsForReappointment(minedBlocks) *
                                   historyDict[minerInRound.Key].ContinualAppointmentCount /
                                   totalReappointment);

                State.DividendContract.SendDividends.Send(
                    new SendDividendsInput
                    {
                        To = Address.FromPublicKey(ByteArrayHelpers.FromHexString(minerInRound.Key)),
                        Amount = amount
                    });
            }

            if (TryToGetBackups(roundInformation.RealTimeMinersInformation.Keys.ToList(), out var backups))
            {
                foreach (var backup in backups)
                {
                    var backupCount = (long) backups.Count;
                    var amount = backupCount == 0 ? 0 : GetDividendsForBackupNodes(minedBlocks) / backupCount;
                    State.DividendContract.SendDividends.Send(
                        new SendDividendsInput
                        {
                            To = Address.FromPublicKey(ByteArrayHelpers.FromHexString(backup)),
                            Amount = amount
                        });
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