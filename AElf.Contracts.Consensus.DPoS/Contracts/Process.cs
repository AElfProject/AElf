using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using AElf.Common;
using AElf.Kernel;
using Api = AElf.Sdk.CSharp.Api;

// ReSharper disable once CheckNamespace
namespace AElf.Contracts.Consensus.DPoS
{
    // ReSharper disable UnusedMember.Global
    // ReSharper disable InconsistentNaming
    public class Process
    {
        private int LogLevel { get; set; } = 0;

        private readonly IDPoSDataHelper _dataHelper;

        public Process(IDPoSDataHelper dataHelper)
        {
            _dataHelper = dataHelper;
        }

        public void InitialTerm(Term firstTerm)
        {
            InitialBlockchain(firstTerm);

            InitialMainchainToken();

            SetAliases(firstTerm);

            var senderPublicKey = Api.RecoverPublicKey().ToHex();
            
            // Update ProducedBlocks for sender.
            if (firstTerm.FirstRound.RealTimeMinersInfo.ContainsKey(senderPublicKey))
            {
                firstTerm.FirstRound.RealTimeMinersInfo[senderPublicKey].ProducedBlocks += 1;
            }
            else
            {
                // The sender isn't a initial miner, need to update its history information.
                if (_dataHelper.TryToGetMinerHistoryInformation(senderPublicKey, out var historyInformation))
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

                _dataHelper.AddOrUpdateMinerHistoryInformation(historyInformation);
            }

            firstTerm.FirstRound.BlockchainAge = 1;
            firstTerm.SecondRound.BlockchainAge = 1;
            _dataHelper.TryToAddRoundInformation(firstTerm.FirstRound);
            _dataHelper.TryToAddRoundInformation(firstTerm.SecondRound);
        }

        public ActionResult NextTerm(Term term)
        {
            var stopwatch = new Stopwatch();
            stopwatch.Start();
            
            // Count missed time slot of current round.
            CountMissedTimeSlots();

            Api.Assert(_dataHelper.TryToGetTermNumber(out var termNumber), "Term number not found.");
            Api.SendInline(Api.DividendsContractAddress, "KeepWeights", termNumber);

            // Update current term number and current round number.
            Api.Assert(_dataHelper.TryToUpdateTermNumber(term.TermNumber), "Failed to update term number.");
            Api.Assert(_dataHelper.TryToUpdateRoundNumber(term.FirstRound.RoundNumber), "Failed to update round number.");

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

            var senderPublicKey = Api.RecoverPublicKey().ToHex();

            // Update produced block number of this node.
            if (term.FirstRound.RealTimeMinersInfo.ContainsKey(senderPublicKey))
            {
                term.FirstRound.RealTimeMinersInfo[senderPublicKey].ProducedBlocks += 1;
            }
            else
            {
                if (_dataHelper.TryToGetMinerHistoryInformation(senderPublicKey, out var historyInformation))
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

                _dataHelper.AddOrUpdateMinerHistoryInformation(historyInformation);
            }

            // Update miners list.
            _dataHelper.SetMiners(term.Miners);

            // Update term number lookup. (Using term number to get first round number of related term.)
            _dataHelper.AddTermNumberToFirstRoundNumber(term.TermNumber, term.FirstRound.RoundNumber);

            Api.Assert(_dataHelper.TryToGetCurrentAge(out var blockAge), "Block age not found.");
            // Update blockchain age of next two rounds.
            term.FirstRound.BlockchainAge = blockAge;
            term.SecondRound.BlockchainAge = blockAge;

            // Update rounds information of next two rounds.
            _dataHelper.TryToAddRoundInformation(term.FirstRound);
            _dataHelper.TryToAddRoundInformation(term.SecondRound);
            
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
        public ActionResult SnapshotForTerm(ulong snapshotTermNumber, ulong lastRoundNumber)
        {
            var stopwatch = new Stopwatch();
            stopwatch.Start();
            
            if (_dataHelper.TryToGetSnapshot(snapshotTermNumber, out _))
            {
                return new ActionResult
                {
                    Success = false,
                    ErrorMessage = $"Snapshot of term {snapshotTermNumber} already taken."
                };
            }

            if (!_dataHelper.TryToGetRoundInformation(lastRoundNumber, out var roundInformation))
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
            if (_dataHelper.TryToGetVictories(out var victories))
            {
                foreach (var candidatePublicKey in victories.PublicKeys)
                {
                    if (_dataHelper.TryToGetTicketsInformation(candidatePublicKey, out var candidateTickets))
                    {
                        candidateInTerms.Add(new CandidateInTerm
                        {
                            PublicKey = candidatePublicKey,
                            Votes = candidateTickets.ObtainedTickets
                        });
                    }
                    else
                    {
                        _dataHelper.AddOrUpdateTicketsInformation(new Tickets {PublicKey = candidatePublicKey});
                        candidateInTerms.Add(new CandidateInTerm
                        {
                            PublicKey = candidatePublicKey,
                            Votes = 0
                        });
                    }
                }
            }

            Api.Assert(_dataHelper.TryToGetRoundNumber(out var roundNumber), "Round number not found.");
            // Set snapshot of related term.
            _dataHelper.SetSnapshot(new TermSnapshot
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

        public ActionResult SnapshotForMiners(ulong previousTermNumber, ulong lastRoundNumber)
        {
            var stopwatch = new Stopwatch();
            stopwatch.Start();
            
            Api.Assert(_dataHelper.TryToGetRoundInformation(lastRoundNumber, out var roundInformation),
                "Round information not found.");

            foreach (var candidate in roundInformation.RealTimeMinersInfo)
            {
                CandidateInHistory candidateInHistory;
                if (_dataHelper.TryToGetMinerHistoryInformation(candidate.Key, out var historyInformation))
                {
                    var terms = new List<ulong>(historyInformation.Terms.ToList());

                    if (terms.Contains(previousTermNumber))
                    {
                        return new ActionResult
                            {Success = false, ErrorMessage = "Snapshot for miners in previous term already taken."};
                    }

                    terms.Add(previousTermNumber);

                    var continualAppointmentCount = historyInformation.ContinualAppointmentCount;
                    if (_dataHelper.TryToGetMiners(previousTermNumber, out var minersOfLastTerm) &&
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

                _dataHelper.AddOrUpdateMinerHistoryInformation(candidateInHistory);
            }
            
            Console.WriteLine($"Miners snapshot duration: {stopwatch.ElapsedMilliseconds} ms.");

            return new ActionResult {Success = true};
        }

        public ActionResult SendDividends(ulong dividendsTermNumber, ulong lastRoundNumber)
        {
            var stopwatch = new Stopwatch();
            stopwatch.Start();
            
            Api.Assert(_dataHelper.TryToGetRoundInformation(lastRoundNumber, out var roundInformation),
                "Round information not found.");

            // Set dividends of related term to Dividends Contract.
            var minedBlocks = roundInformation.RealTimeMinersInfo.Values.Aggregate<MinerInRound, ulong>(0,
                (current, minerInRound) => current + minerInRound.ProducedBlocks);
            Api.SendInline(Api.DividendsContractAddress, "AddDividends", dividendsTermNumber,
                Config.GetDividendsForVoters(minedBlocks));

            ulong totalVotes = 0;
            ulong totalReappointment = 0;
            var continualAppointmentDict = new Dictionary<string, ulong>();
            foreach (var minerInRound in roundInformation.RealTimeMinersInfo)
            {
                if (_dataHelper.TryToGetTicketsInformation(minerInRound.Key, out var candidateTickets))
                {
                    totalVotes += candidateTickets.ObtainedTickets;
                }

                if (_dataHelper.TryToGetMinerHistoryInformation(minerInRound.Key, out var candidateInHistory))
                {
                    totalReappointment += candidateInHistory.ContinualAppointmentCount;
                    
                    continualAppointmentDict.Add(minerInRound.Key, candidateInHistory.ContinualAppointmentCount);
                }
                
                // Transfer dividends for actual miners. (The miners list based on last round of current term.)
                Api.SendDividends(
                    Address.FromPublicKey(ByteArrayHelpers.FromHexString(minerInRound.Key)),
                    Config.GetDividendsForEveryMiner(minedBlocks) +
                    (totalVotes == 0
                        ? 0
                        : Config.GetDividendsForTicketsCount(minedBlocks) * candidateTickets.ObtainedTickets / totalVotes) +
                    (totalReappointment == 0
                        ? 0
                        : Config.GetDividendsForReappointment(minedBlocks) * continualAppointmentDict[minerInRound.Key] /
                          totalReappointment));
            }

            if (_dataHelper.TryToGetBackups(roundInformation.RealTimeMinersInfo.Keys.ToList(), out var backups))
            {
                foreach (var backup in backups)
                {
                    var backupCount = (ulong) backups.Count;
                    Api.SendDividends(
                        Address.FromPublicKey(ByteArrayHelpers.FromHexString(backup)),
                        backupCount == 0 ? 0 : Config.GetDividendsForBackupNodes(minedBlocks) / backupCount);
                }
            }
            
            Console.WriteLine($"Send dividends duration: {stopwatch.ElapsedMilliseconds} ms.");

            return new ActionResult {Success = true};
        }

        public void NextRound(Forwarding forwarding)
        {
            var stopwatch = new Stopwatch();
            stopwatch.Start();

            Api.Assert(
                forwarding.NextRound.RoundNumber == 0 || _dataHelper.TryToGetRoundNumber(out var roundNumber) &&
                roundNumber < forwarding.NextRound.RoundNumber, "Incorrect round number for next round.");

            var senderPublicKey = Api.RecoverPublicKey().ToHex();
            
            if ( _dataHelper.TryToGetCurrentRoundInformation(out var currentRoundInformation) &&
                forwarding.NextRound.GetMinersHash() != currentRoundInformation.GetMinersHash() &&
                forwarding.NextRound.RealTimeMinersInfo.Keys.Count == GlobalConfig.BlockProducerNumber &&
                _dataHelper.TryToGetTermNumber(out var termNumber))
            {
                var miners = forwarding.NextRound.RealTimeMinersInfo.Keys.ToMiners();
                miners.TermNumber = termNumber;
                _dataHelper.SetMiners(miners, true);
            }

            // Update the age of this blockchain
            _dataHelper.SetBlockAge(forwarding.CurrentAge);

            // Check whether round id matched.
            var forwardingCurrentRound = forwarding.CurrentRound;
            _dataHelper.TryToGetCurrentRoundInformation(out var currentRound);
            Api.Assert(forwardingCurrentRound.RoundId == currentRound.RoundId, GlobalConfig.RoundIdNotMatched);

            var completeCurrentRoundInfo = SupplyCurrentRoundInfo(currentRound, forwardingCurrentRound);

            Api.Assert(_dataHelper.TryToGetCurrentAge(out var blockAge), "Block age not found.");
            
            Api.Assert(_dataHelper.TryToGetRoundNumber(out var currentRoundNumber), "Round number not found.");

            // When the node is in Round 1, gonna just update the data of Round 2 instead of re-generate orders.
            if (forwarding.NextRound.RoundNumber == 0)
            {
                if (_dataHelper.TryToGetRoundInformation(currentRound.RoundNumber + 1, out var nextRound))
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
                        if (_dataHelper.TryToGetMinerHistoryInformation(senderPublicKey, out var historyInformation))
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

                        _dataHelper.AddOrUpdateMinerHistoryInformation(historyInformation);
                    }

                    _dataHelper.TryToAddRoundInformation(nextRound);
                    _dataHelper.SetRoundNumber(2);
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
                        if (_dataHelper.TryToGetMinerHistoryInformation(senderPublicKey, out var historyInformation))
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

                        _dataHelper.AddOrUpdateMinerHistoryInformation(historyInformation);
                    }
                }

                forwarding.NextRound.BlockchainAge = blockAge;
                if (forwarding.NextRound.RealTimeMinersInfo.ContainsKey(senderPublicKey))
                {
                    forwarding.NextRound.RealTimeMinersInfo[senderPublicKey].ProducedBlocks += 1;
                }
                else
                {
                    if (_dataHelper.TryToGetMinerHistoryInformation(senderPublicKey, out var historyInformation))
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

                    _dataHelper.AddOrUpdateMinerHistoryInformation(historyInformation);
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
                        Api.Assert(_dataHelper.TryToGetRoundInformation(i, out var round),
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

                _dataHelper.TryToAddRoundInformation(forwarding.NextRound);
                _dataHelper.TryToUpdateRoundNumber(forwarding.NextRound.RoundNumber);
            }
            
            Console.WriteLine($"Round changing duration: {stopwatch.ElapsedMilliseconds} ms.");
        }

        public void PackageOutValue(ToPackage toPackage)
        {
            Api.Assert(_dataHelper.TryToGetCurrentRoundInformation(out var currentRound) && 
                       toPackage.RoundId == currentRound.RoundId, GlobalConfig.RoundIdNotMatched);

            Api.Assert(_dataHelper.TryToGetCurrentRoundInformation(out var roundInformation),
                "Round information not found.");

            if (roundInformation.RoundNumber != 1)
            {
                roundInformation.RealTimeMinersInfo[Api.RecoverPublicKey().ToHex()].Signature = toPackage.Signature;
            }

            roundInformation.RealTimeMinersInfo[Api.RecoverPublicKey().ToHex()].OutValue = toPackage.OutValue;

            roundInformation.RealTimeMinersInfo[Api.RecoverPublicKey().ToHex()].ProducedBlocks += 1;

            _dataHelper.TryToAddRoundInformation(roundInformation);
        }

        public void BroadcastInValue(ToBroadcast toBroadcast)
        {
            if (_dataHelper.TryToGetPreviousRoundInformation(out var previousRound) && 
                toBroadcast.RoundId != previousRound.RoundId)
            {
                return;
            }

            Api.Assert(_dataHelper.TryToGetPreviousRoundInformation(out var previousRoundInformation),
                "Round information not found.");
            Api.Assert(previousRoundInformation.RealTimeMinersInfo[Api.RecoverPublicKey().ToHex()].OutValue != null,
                GlobalConfig.OutValueIsNull);
            Api.Assert(previousRoundInformation.RealTimeMinersInfo[Api.RecoverPublicKey().ToHex()].Signature != null,
                GlobalConfig.SignatureIsNull);
            Api.Assert(
                previousRoundInformation.RealTimeMinersInfo[Api.RecoverPublicKey().ToHex()].OutValue ==
                Hash.FromMessage(toBroadcast.InValue),
                GlobalConfig.InValueNotMatchToOutValue);

            previousRoundInformation.RealTimeMinersInfo[Api.RecoverPublicKey().ToHex()].InValue = toBroadcast.InValue;

            _dataHelper.TryToAddRoundInformation(previousRoundInformation);
        }

        #region Vital Steps

        private void InitialBlockchain(Term firstTerm)
        {
            _dataHelper.SetChainId(firstTerm.ChainId);
            _dataHelper.SetTermNumber(1);
            _dataHelper.SetRoundNumber(1);
            _dataHelper.SetBlockAge(1);
            _dataHelper.AddTermNumberToFirstRoundNumber(1, 1);
            _dataHelper.SetBlockchainStartTimestamp(firstTerm.Timestamp);
            _dataHelper.SetMiners(firstTerm.Miners);
            _dataHelper.SetMiningInterval(firstTerm.MiningInterval);
        }

        private void InitialMainchainToken()
        {
            Api.SendInline(Api.TokenContractAddress, "Initialize", "ELF", "AElf Token", GlobalConfig.TotalSupply, 2);
        }

        private void SetAliases(Term term)
        {
            var index = 0;
            
            foreach (var publicKey in term.Miners.PublicKeys)
            {
                if (index >= Config.Aliases.Count)
                    return;

                var alias = Config.Aliases[index];
                _dataHelper.SetAlias(publicKey, alias);
                _dataHelper.AddOrUpdateMinerHistoryInformation(new CandidateInHistory
                    {PublicKey = publicKey, CurrentAlias = alias});
                index++;
            }
            /*

            if (IsMainchain())
            {
                foreach (var publicKey in term.Miners.PublicKeys)
                {
                    if (index >= Config.Aliases.Count)
                        return;

                    var alias = Config.Aliases[index];
                    _dataHelper.SetAlias(publicKey, alias);
                    _dataHelper.AddOrUpdateMinerHistoryInformation(new CandidateInHistory
                        {PublicKey = publicKey, CurrentAlias = alias});
                    index++;
                }
            }
            else
            {
                foreach (var publicKey in term.Miners.PublicKeys)
                {
                    var alias = publicKey.Substring(0, GlobalConfig.AliasLimit);
                    _dataHelper.SetAlias(publicKey, alias);
                    ConsoleWriteLine(nameof(SetAliases), $"Set alias {alias} to {publicKey}");
                }
            }*/
        }

        /// <summary>
        /// Can only supply signature, out value, in value if one missed his time slot.
        /// </summary>
        /// <param name="currentRound"></param>
        /// <param name="forwardingCurrentRound"></param>
        private Round SupplyCurrentRoundInfo(Round currentRound, Round forwardingCurrentRound)
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

            _dataHelper.TryToUpdateRoundInformation(currentRound);

            return currentRound;
        }

        #endregion

        public IEnumerable<string> GetVictories()
        {
            if (_dataHelper.TryToGetVictories(out var victories))
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
            if (_dataHelper.TryToGetCurrentRoundInformation(out var currentRound))
            {
                foreach (var minerInRound in currentRound.RealTimeMinersInfo)
                {
                    if (minerInRound.Value.OutValue == null)
                    {
                        minerInRound.Value.MissedTimeSlots += 1;
                    }
                }

                _dataHelper.TryToUpdateRoundInformation(currentRound);
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