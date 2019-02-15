using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using AElf.Common;
using AElf.Kernel;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Contracts.Consensus.DPoS
{
    public partial class Contract
    {
        private int LogLevel { get; set; } = 0;

        public void Process_InitialTerm(Term firstTerm)
        {
            InitialBlockchain(firstTerm);

            InitialMainchainToken();

            SetAliases(firstTerm);

            var senderPublicKey = Context.RecoverPublicKey().ToHex();

            // Update ProducedBlocks for sender.
            if (firstTerm.FirstRound.RealTimeMinersInfo.ContainsKey(senderPublicKey))
            {
                firstTerm.FirstRound.RealTimeMinersInfo[senderPublicKey].ProducedBlocks += 1;
            }
            else
            {
                // The sender isn't a initial miner, need to update its history information.
                if (TryToGetMinerHistoryInformation(senderPublicKey, out var historyInformation))
                {
                    historyInformation.ProducedBlocks += 1;
                }
                else
                {
                    // Create a new history information.
                    historyInformation = new CandidateInHistory
                    {
                        PublicKey = senderPublicKey,
                        ProducedBlocks = 1,
                        CurrentAlias = senderPublicKey.Substring(0, GlobalConfig.AliasLimit)
                    };
                }

                AddOrUpdateMinerHistoryInformation(historyInformation);
            }

            firstTerm.FirstRound.BlockchainAge = 1;
            firstTerm.SecondRound.BlockchainAge = 1;
            TryToAddRoundInformation(firstTerm.FirstRound);
            TryToAddRoundInformation(firstTerm.SecondRound);
        }

        public ActionResult Process_NextTerm(Term term)
        {
            var stopwatch = new Stopwatch();
            stopwatch.Start();

            // Count missed time slot of current round.
            CountMissedTimeSlots();

            Assert(TryToGetTermNumber(out var termNumber), "Term number not found.");
            State.DividendContract.KeepWeights(termNumber);

            // Update current term number and current round number.
            Assert(TryToUpdateTermNumber(term.TermNumber), "Failed to update term number.");
            Assert(TryToUpdateRoundNumber(term.FirstRound.RoundNumber), "Failed to update round number.");

            // Reset some fields of first two rounds of next term.
            foreach (var minerInRound in term.FirstRound.RealTimeMinersInfo.Values)
            {
                minerInRound.MissedTimeSlots = 0;
                minerInRound.ProducedBlocks = 0;
            }

            foreach (var minerInRound in term.SecondRound.RealTimeMinersInfo.Values)
            {
                minerInRound.MissedTimeSlots = 0;
                minerInRound.ProducedBlocks = 0;
            }

            var senderPublicKey = Context.RecoverPublicKey().ToHex();

            // Update produced block number of this node.
            if (term.FirstRound.RealTimeMinersInfo.ContainsKey(senderPublicKey))
            {
                term.FirstRound.RealTimeMinersInfo[senderPublicKey].ProducedBlocks += 1;
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
                        CurrentAlias = senderPublicKey.Substring(0, GlobalConfig.AliasLimit)
                    };
                }

                AddOrUpdateMinerHistoryInformation(historyInformation);
            }

            // Update miners list.
            SetMiners(term.Miners);

            // Update term number lookup. (Using term number to get first round number of related term.)
            AddTermNumberToFirstRoundNumber(term.TermNumber, term.FirstRound.RoundNumber);

            Assert(TryToGetCurrentAge(out var blockAge), "Block age not found.");
            // Update blockchain age of next two rounds.
            term.FirstRound.BlockchainAge = blockAge;
            term.SecondRound.BlockchainAge = blockAge;

            // Update rounds information of next two rounds.
            TryToAddRoundInformation(term.FirstRound);
            TryToAddRoundInformation(term.SecondRound);

            Console.WriteLine($"Term changing duration: {stopwatch.ElapsedMilliseconds} ms.");

            return new ActionResult {Success = true};
        }

        /// <summary>
        /// Take a snapshot of specific term.
        /// Basically this snapshot is used for getting ranks of candidates of specific term.
        /// </summary>
        /// <param name="snapshotTermNumber"></param>
        /// <param name="lastRoundNumber"></param>
        /// <returns></returns>
        public ActionResult Process_SnapshotForTerm(ulong snapshotTermNumber, ulong lastRoundNumber)
        {
            var stopwatch = new Stopwatch();
            stopwatch.Start();

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
            var minedBlocks = roundInformation.RealTimeMinersInfo.Values.Aggregate<MinerInRound, ulong>(0,
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

            Console.WriteLine($"Term snapshot duration: {stopwatch.ElapsedMilliseconds} ms.");

            return new ActionResult {Success = true};
        }

        public ActionResult Process_SnapshotForMiners(ulong previousTermNumber, ulong lastRoundNumber)
        {
            var stopwatch = new Stopwatch();
            stopwatch.Start();

            Assert(TryToGetRoundInformation(lastRoundNumber, out var roundInformation),
                "Round information not found.");

            foreach (var candidate in roundInformation.RealTimeMinersInfo)
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

            Console.WriteLine($"Miners snapshot duration: {stopwatch.ElapsedMilliseconds} ms.");

            return new ActionResult {Success = true};
        }

        public ActionResult Process_SendDividends(ulong dividendsTermNumber, ulong lastRoundNumber)
        {
            var stopwatch = new Stopwatch();
            stopwatch.Start();

            Assert(TryToGetRoundInformation(lastRoundNumber, out var roundInformation),
                "Round information not found.");

            // Set dividends of related term to Dividends Contract.
            var minedBlocks = roundInformation.RealTimeMinersInfo.Values.Aggregate<MinerInRound, ulong>(0,
                (current, minerInRound) => current + minerInRound.ProducedBlocks);
            State.DividendContract.AddDividends(dividendsTermNumber, Config.GetDividendsForVoters(minedBlocks));

            ulong totalVotes = 0;
            ulong totalReappointment = 0;
            var continualAppointmentDict = new Dictionary<string, ulong>();
            foreach (var minerInRound in roundInformation.RealTimeMinersInfo)
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
                var amount = Config.GetDividendsForEveryMiner(minedBlocks) +
                             (totalVotes == 0
                                 ? 0
                                 : Config.GetDividendsForTicketsCount(minedBlocks) * candidateTickets.ObtainedTickets /
                                   totalVotes) +
                             (totalReappointment == 0
                                 ? 0
                                 : Config.GetDividendsForReappointment(minedBlocks) *
                                   continualAppointmentDict[minerInRound.Key] /
                                   totalReappointment);
                // TODO: Can we ask the miners to claim the rewards ???
                State.DividendContract.SendDividends(
                    Address.FromPublicKey(ByteArrayHelpers.FromHexString(minerInRound.Key)), amount);
            }

            if (TryToGetBackups(roundInformation.RealTimeMinersInfo.Keys.ToList(), out var backups))
            {
                foreach (var backup in backups)
                {
                    var backupCount = (ulong) backups.Count;
                    var amount = backupCount == 0 ? 0 : Config.GetDividendsForBackupNodes(minedBlocks) / backupCount;
                    State.DividendContract.SendDividends(Address.FromPublicKey(ByteArrayHelpers.FromHexString(backup)),
                        amount);
                }
            }

            Console.WriteLine($"Send dividends duration: {stopwatch.ElapsedMilliseconds} ms.");

            return new ActionResult {Success = true};
        }

        public void Process_NextRound(Forwarding forwarding)
        {
            var stopwatch = new Stopwatch();
            stopwatch.Start();

            Assert(
                forwarding.NextRound.RoundNumber == 0 || TryToGetRoundNumber(out var roundNumber) &&
                roundNumber < forwarding.NextRound.RoundNumber, "Incorrect round number for next round.");

            var senderPublicKey = Context.RecoverPublicKey().ToHex();

            if (TryToGetCurrentRoundInformation(out var currentRoundInformation) &&
                forwarding.NextRound.GetMinersHash() != currentRoundInformation.GetMinersHash() &&
                forwarding.NextRound.RealTimeMinersInfo.Keys.Count == GlobalConfig.BlockProducerNumber &&
                TryToGetTermNumber(out var termNumber))
            {
                var miners = forwarding.NextRound.RealTimeMinersInfo.Keys.ToMiners();
                miners.TermNumber = termNumber;
                SetMiners(miners, true);
            }

            // Update the age of this blockchain
            SetBlockAge(forwarding.CurrentAge);

            // Check whether round id matched.
            var forwardingCurrentRound = forwarding.CurrentRound;
            TryToGetCurrentRoundInformation(out var currentRound);
            Assert(forwardingCurrentRound.RoundId == currentRound.RoundId, GlobalConfig.RoundIdNotMatched);

            var completeCurrentRoundInfo = Process_SupplyCurrentRoundInfo(currentRound, forwardingCurrentRound);

            Assert(TryToGetCurrentAge(out var blockAge), "Block age not found.");

            Assert(TryToGetRoundNumber(out var currentRoundNumber), "Round number not found.");

            // When the node is in Round 1, gonna just update the data of Round 2 instead of re-generate orders.
            if (forwarding.NextRound.RoundNumber == 0)
            {
                if (TryToGetRoundInformation(currentRound.RoundNumber + 1, out var nextRound))
                {
                    foreach (var minerInRound in completeCurrentRoundInfo.RealTimeMinersInfo)
                    {
                        nextRound.RealTimeMinersInfo[minerInRound.Key].MissedTimeSlots =
                            minerInRound.Value.MissedTimeSlots;
                        nextRound.RealTimeMinersInfo[minerInRound.Key].ProducedBlocks =
                            minerInRound.Value.ProducedBlocks;
                    }

                    nextRound.BlockchainAge = blockAge;
                    if (forwarding.NextRound.RealTimeMinersInfo.ContainsKey(senderPublicKey))
                    {
                        nextRound.RealTimeMinersInfo[senderPublicKey].ProducedBlocks += 1;
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
                                CurrentAlias = senderPublicKey.Substring(0, GlobalConfig.AliasLimit)
                            };
                        }

                        AddOrUpdateMinerHistoryInformation(historyInformation);
                    }

                    TryToAddRoundInformation(nextRound);
                    SetRoundNumber(2);
                }
            }
            else
            {
                // Update missed time slots and produced blocks for each miner.
                foreach (var minerInRound in completeCurrentRoundInfo.RealTimeMinersInfo)
                {
                    if (forwarding.NextRound.RealTimeMinersInfo.ContainsKey(minerInRound.Key))
                    {
                        forwarding.NextRound.RealTimeMinersInfo[minerInRound.Key].MissedTimeSlots =
                            minerInRound.Value.MissedTimeSlots;
                        forwarding.NextRound.RealTimeMinersInfo[minerInRound.Key].ProducedBlocks =
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
                                CurrentAlias = senderPublicKey.Substring(0, GlobalConfig.AliasLimit)
                            };
                        }

                        AddOrUpdateMinerHistoryInformation(historyInformation);
                    }
                }

                forwarding.NextRound.BlockchainAge = blockAge;
                if (forwarding.NextRound.RealTimeMinersInfo.ContainsKey(senderPublicKey))
                {
                    forwarding.NextRound.RealTimeMinersInfo[senderPublicKey].ProducedBlocks += 1;
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
                            CurrentAlias = senderPublicKey.Substring(0, GlobalConfig.AliasLimit)
                        };
                    }

                    AddOrUpdateMinerHistoryInformation(historyInformation);
                }

                if (currentRoundNumber > GlobalConfig.ForkDetectionRoundNumber)
                {
                    foreach (var minerInRound in forwarding.NextRound.RealTimeMinersInfo)
                    {
                        minerInRound.Value.LatestMissedTimeSlots = 0;
                    }

                    var rounds = new List<Round>();
                    for (var i = currentRoundNumber - GlobalConfig.ForkDetectionRoundNumber + 1;
                        i <= currentRoundNumber;
                        i++)
                    {
                        Assert(TryToGetRoundInformation(i, out var round),
                            GlobalConfig.RoundNumberNotFound);
                        rounds.Add(round);
                    }

                    foreach (var round in rounds)
                    {
                        foreach (var minerInRound in round.RealTimeMinersInfo)
                        {
                            if (minerInRound.Value.IsMissed &&
                                forwarding.NextRound.RealTimeMinersInfo.ContainsKey(minerInRound.Key))
                            {
                                forwarding.NextRound.RealTimeMinersInfo[minerInRound.Key].LatestMissedTimeSlots +=
                                    1;
                            }

                            if (!minerInRound.Value.IsMissed &&
                                forwarding.NextRound.RealTimeMinersInfo.ContainsKey(minerInRound.Key))
                            {
                                forwarding.NextRound.RealTimeMinersInfo[minerInRound.Key].LatestMissedTimeSlots = 0;
                            }
                        }
                    }
                }

                TryToAddRoundInformation(forwarding.NextRound);
                TryToUpdateRoundNumber(forwarding.NextRound.RoundNumber);
            }

            Console.WriteLine($"Round changing duration: {stopwatch.ElapsedMilliseconds} ms.");
        }

        public void Process_PackageOutValue(ToPackage toPackage)
        {
            Assert(TryToGetCurrentRoundInformation(out var currentRound) &&
                   toPackage.RoundId == currentRound.RoundId, GlobalConfig.RoundIdNotMatched);

            Assert(TryToGetCurrentRoundInformation(out var roundInformation),
                "Round information not found.");

            if (roundInformation.RoundNumber != 1)
            {
                roundInformation.RealTimeMinersInfo[Context.RecoverPublicKey().ToHex()].Signature = toPackage.Signature;
            }

            roundInformation.RealTimeMinersInfo[Context.RecoverPublicKey().ToHex()].OutValue = toPackage.OutValue;

            roundInformation.RealTimeMinersInfo[Context.RecoverPublicKey().ToHex()].ProducedBlocks += 1;

            TryToAddRoundInformation(roundInformation);
        }

        public void Process_BroadcastInValue(ToBroadcast toBroadcast)
        {
            if (TryToGetCurrentRoundInformation(out var currentRound) &&
                toBroadcast.RoundId != currentRound.RoundId)
            {
                return;
            }

            Assert(TryToGetCurrentRoundInformation(out var roundInformation),
                "Round information not found.");
            Assert(roundInformation.RealTimeMinersInfo[Context.RecoverPublicKey().ToHex()].OutValue != null,
                GlobalConfig.OutValueIsNull);
            Assert(roundInformation.RealTimeMinersInfo[Context.RecoverPublicKey().ToHex()].Signature != null,
                GlobalConfig.SignatureIsNull);
            Assert(
                roundInformation.RealTimeMinersInfo[Context.RecoverPublicKey().ToHex()].OutValue ==
                Hash.FromMessage(toBroadcast.InValue),
                GlobalConfig.InValueNotMatchToOutValue);

            roundInformation.RealTimeMinersInfo[Context.RecoverPublicKey().ToHex()].InValue = toBroadcast.InValue;

            TryToAddRoundInformation(roundInformation);
        }

        #region Vital Steps

        private void InitialBlockchain(Term firstTerm)
        {
            SetChainId(firstTerm.ChainId);
            SetTermNumber(1);
            SetRoundNumber(1);
            SetBlockAge(1);
            AddTermNumberToFirstRoundNumber(1, 1);
            SetBlockchainStartTimestamp(firstTerm.Timestamp);
            SetMiners(firstTerm.Miners);
            SetMiningInterval(firstTerm.MiningInterval);
        }

        private void InitialMainchainToken()
        {
            State.TokenContract.Initialize("ELF", "AElf Token", GlobalConfig.TotalSupply, 2);
        }

        private void SetAliases(Term term)
        {
            var index = 0;
            foreach (var publicKey in term.Miners.PublicKeys)
            {
                if (index >= Config.InitialMinersAliases.Count)
                    return;

                var alias = Config.InitialMinersAliases[index];
                SetAlias(publicKey, alias);
                AddOrUpdateMinerHistoryInformation(new CandidateInHistory
                    {PublicKey = publicKey, CurrentAlias = alias});
                index++;
            }
        }

        /// <summary>
        /// Can only supply signature, out value, in value if one missed his time slot.
        /// </summary>
        /// <param name="currentRound"></param>
        /// <param name="forwardingCurrentRound"></param>
        private Round Process_SupplyCurrentRoundInfo(Round currentRound, Round forwardingCurrentRound)
        {
            foreach (var suppliedMiner in forwardingCurrentRound.RealTimeMinersInfo)
            {
                if (suppliedMiner.Value.MissedTimeSlots >
                    currentRound.RealTimeMinersInfo[suppliedMiner.Key].MissedTimeSlots
                    && currentRound.RealTimeMinersInfo[suppliedMiner.Key].OutValue == null)
                {
                    currentRound.RealTimeMinersInfo[suppliedMiner.Key].OutValue = suppliedMiner.Value.OutValue;
                    currentRound.RealTimeMinersInfo[suppliedMiner.Key].InValue = suppliedMiner.Value.InValue;
                    currentRound.RealTimeMinersInfo[suppliedMiner.Key].Signature = suppliedMiner.Value.Signature;

                    currentRound.RealTimeMinersInfo[suppliedMiner.Key].MissedTimeSlots += 1;
                    currentRound.RealTimeMinersInfo[suppliedMiner.Key].IsMissed = true;
                }
            }

            TryToUpdateRoundInformation(currentRound);

            return currentRound;
        }

        #endregion

        public IEnumerable<string> GetVictories()
        {
            if (TryToGetVictories(out var victories))
            {
                return victories.PublicKeys;
            }

            return null;
        }

        /// <summary>
        /// Normally this process contained in NextRound method.
        /// </summary>
        private void CountMissedTimeSlots()
        {
            if (TryToGetCurrentRoundInformation(out var currentRound))
            {
                foreach (var minerInRound in currentRound.RealTimeMinersInfo)
                {
                    if (minerInRound.Value.OutValue == null)
                    {
                        minerInRound.Value.MissedTimeSlots += 1;
                    }
                }

                TryToUpdateRoundInformation(currentRound);
            }
        }

        /// <summary>
        /// Debug level:
        /// 6 = Off
        /// 5 = Fatal
        /// 4 = Error
        /// 3 = Warn
        /// 2 = Info
        /// 1 = Debug
        /// 0 = Trace
        /// </summary>
        /// <param name="prefix"></param>
        /// <param name="log"></param>
        /// <param name="ex"></param>
        private void ConsoleWriteLine(string prefix, string log, Exception ex = null)
        {
            if (LogLevel == 6)
                return;

            Console.WriteLine(
                $"[{DateTime.UtcNow.ToLocalTime():yyyy-MM-dd HH:mm:ss.fff} - Consensus]{prefix} - {log}.");
            if (ex != null)
            {
                Console.WriteLine(ex);
            }
        }
    }
}