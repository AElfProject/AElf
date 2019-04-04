using System;
using System.Collections.Generic;
using System.Linq;
using AElf.Common;
using AElf.Consensus.DPoS;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Contracts.Consensus.DPoS
{
    public partial class ConsensusContract
    {
        private void SetInitialMinersAliases(IEnumerable<string> publicKeys)
        {
            var index = 0;
            var aliases = DPoSContractConsts.InitialMinersAliases.Split(',');
            foreach (var publicKey in publicKeys)
            {
                if (index >= aliases.Length)
                    return;

                var alias = aliases[index];
                SetAlias(publicKey, alias);
                index++;
            }
        }
        
        private void SetAlias(string publicKey, string alias)
        {
            State.AliasesMap[publicKey.ToStringValue()] = alias.ToStringValue();
            State.AliasesLookupMap[alias.ToStringValue()] = publicKey.ToStringValue();
        }

        private void UpdateBlockchainAge(long age)
        {
            //Assert(State.AgeField.Value <= age,
                //ContractErrorCode.GetErrorMessage(ContractErrorCode.AttemptFailed, "Cannot decrease blockchain age."));
            State.AgeField.Value = age;
        }

        private bool TryToUpdateTermNumber(long termNumber)
        {
            var oldTermNumber = State.CurrentTermNumberField.Value;
            if (termNumber != 1 && oldTermNumber + 1 != termNumber)
            {
                return false;
            }

            State.CurrentTermNumberField.Value = termNumber;
            return true;
        }

        public bool TryToGetTermNumber(out long termNumber)
        {
            termNumber = State.CurrentTermNumberField.Value;
            return termNumber != 0;
        }

        public bool TryToGetMiners(long termNumber, out Miners miners)
        {
            miners = State.MinersMap[termNumber.ToInt64Value()];
            return miners != null;
        }

        public bool TryToGetVictories(out Miners victories)
        {
            var candidates = State.CandidatesField.Value;
            if (candidates == null)
            {
                victories = null;
                return false;
            }
            
            var ticketsMap = new Dictionary<string, long>();
            foreach (var candidatePublicKey in candidates.PublicKeys)
            {
                var tickets = State.TicketsMap[candidatePublicKey.ToStringValue()];
                if (tickets != null)
                {
                    ticketsMap.Add(candidatePublicKey, tickets.ObtainedTickets);
                }
            }

            if (ticketsMap.Keys.Count < GetProducerNumber())
            {
                victories = null;
                return false;
            }

            victories = ticketsMap.OrderByDescending(tm => tm.Value).Take(GetProducerNumber())
                .Select(tm => tm.Key)
                .ToList().ToMiners();
            return true;
        }

        public bool TryToGetCurrentAge(out long blockAge)
        {
            blockAge = State.AgeField.Value;
            return blockAge > 0;
        }

        public bool TryToGetMinerHistoryInformation(string publicKey, out CandidateInHistory historyInformation)
        {
            historyInformation = State.HistoryMap[publicKey.ToStringValue()];
            return historyInformation != null;
        }

        public bool TryToGetSnapshot(long termNumber, out TermSnapshot snapshot)
        {
            snapshot = State.SnapshotMap[termNumber.ToInt64Value()];
            return snapshot != null;
        }

        public bool TryToGetTicketsInformation(string publicKey, out Tickets tickets)
        {
            tickets = State.TicketsMap[publicKey.ToStringValue()];
            return tickets != null;
        }

        public bool TryToGetBackups(List<string> currentMiners, out List<string> backups)
        {
            var candidates = State.CandidatesField.Value;
            if (candidates == null)
            {
                backups = null;
                return false;
            }
            backups = candidates.PublicKeys.Except(currentMiners).ToList();
            return backups.Any();
        }

        private void SetTermNumber(long termNumber)
        {
            State.CurrentTermNumberField.Value = termNumber;
        }

        private void SetBlockchainStartTimestamp(Timestamp timestamp)
        {
            Context.LogDebug(() => $"Set start timestamp to {timestamp}");
            State.BlockchainStartTimestamp.Value = timestamp;
        }

        private void AddOrUpdateMinerHistoryInformation(CandidateInHistory historyInformation)
        {
            State.HistoryMap[historyInformation.PublicKey.ToStringValue()] = historyInformation;
        }

        public void AddOrUpdateTicketsInformation(Tickets tickets)
        {
            State.TicketsMap[tickets.PublicKey.ToStringValue()] = tickets;
        }

        public void SetTermSnapshot(TermSnapshot snapshot)
        {
            State.SnapshotMap[snapshot.TermNumber.ToInt64Value()] = snapshot;
        }


        public bool SetMiners(Miners miners, bool gonnaReplaceSomeone = false)
        {
            // Miners for one specific term should only update once.
            var m = State.MinersMap[miners.TermNumber.ToInt64Value()];
            if (gonnaReplaceSomeone || m == null)
            {
                State.MinersMap[miners.TermNumber.ToInt64Value()] = miners;
                return true;
            }

            return false;
        }

        public bool SetSnapshot(TermSnapshot snapshot)
        {
            var s = State.SnapshotMap[snapshot.TermNumber.ToInt64Value()];
            if (s != null)
            {
                return false;
            }

            State.SnapshotMap[snapshot.TermNumber.ToInt64Value()] = snapshot;
            return true;
        }

        public bool IsMinerOfCurrentTerm(string publicKey)
        {
            if (TryToGetTermNumber(out var termNumber))
            {
                if (TryToGetMiners(termNumber, out var miners))
                {
                    return miners.PublicKeys.Contains(publicKey);
                }
            }

            return false;
        }

        #region Utilities

        private bool ValidateMinersList(Round round1, Round round2)
        {
            return round1.RealTimeMinersInformation.Keys.ToList().ToMiners().GetMinersHash() ==
                   round2.RealTimeMinersInformation.Keys.ToList().ToMiners().GetMinersHash();
        }

        private bool OutInValueAreNull(Round round)
        {
            return round.RealTimeMinersInformation.Values.Any(minerInRound =>
                minerInRound.OutValue != null || minerInRound.InValue != null);
        }

        private bool ValidateVictories(Miners miners)
        {
            if (TryToGetVictories(out var victories))
            {
                return victories.GetMinersHash() == miners.GetMinersHash();
            }

            return false;
        }

        private bool IsJustChangedTerm(out long termNumber)
        {
            termNumber = 0;
            return TryToGetPreviousRoundInformation(out var previousRound) &&
                   TryToGetTermNumber(out termNumber) &&
                   previousRound.TermNumber != termNumber;
        }

        private Round GenerateFirstRoundOfNextTerm(string senderPublicKey)
        {
            Round round;
            if (TryToGetTermNumber(out var termNumber) &&
                TryToGetRoundNumber(out var roundNumber) &&
                TryToGetVictories(out var victories) &&
                TryToGetMiningInterval(out var miningInterval))
            {
                round = victories.GenerateFirstRoundOfNewTerm(miningInterval, Context.CurrentBlockTime, roundNumber,
                    termNumber);
            }
            else if (TryToGetCurrentRoundInformation(out round))
            {
                round = round.RealTimeMinersInformation.Keys.ToList().ToMiners()
                    .GenerateFirstRoundOfNewTerm(round.GetMiningInterval(), Context.CurrentBlockTime, round.RoundNumber,
                        termNumber);
            }

            round.BlockchainAge = CurrentAge;

            if (round.RealTimeMinersInformation.ContainsKey(senderPublicKey))
            {
                round.RealTimeMinersInformation[senderPublicKey].ProducedBlocks = 1;
            }
            else
            {
                Assert(TryToGetMinerHistoryInformation(senderPublicKey, out var historyInformation),
                    "Failed to get sender's history information.");
                historyInformation.ProducedBlocks += 1;
                AddOrUpdateMinerHistoryInformation(historyInformation);
            }

            return round;
        }

        #endregion

        public long GetDividendsForEveryMiner(long minedBlocks)
        {
            return (long) (minedBlocks * DPoSContractConsts.ElfTokenPerBlock * DPoSContractConsts.MinersBasicRatio /
                            GetProducerNumber());
        }

        public long GetDividendsForTicketsCount(long minedBlocks)
        {
            return (long) (minedBlocks * DPoSContractConsts.ElfTokenPerBlock * DPoSContractConsts.MinersVotesRatio);
        }

        public long GetDividendsForReappointment(long minedBlocks)
        {
            return (long) (minedBlocks * DPoSContractConsts.ElfTokenPerBlock *
                            DPoSContractConsts.MinersReappointmentRatio);
        }

        public long GetDividendsForBackupNodes(long minedBlocks)
        {
            return (long) (minedBlocks * DPoSContractConsts.ElfTokenPerBlock * DPoSContractConsts.BackupNodesRatio);
        }

        public long GetDividendsForVoters(long minedBlocks)
        {
            return (long) (minedBlocks * DPoSContractConsts.ElfTokenPerBlock * DPoSContractConsts.VotersRatio);
        }

        public int GetProducerNumber()
        {
            var round = GetCurrentRoundInformation(new Empty());
            return round.RealTimeMinersInformation.Count;
            //return 17 + (DateTime.UtcNow.Year - 2019) * 2;
        }

        private void LogVerbose(string log)
        {
            if (State.IsVerbose.Value)
            {
                Context.LogDebug(() => log);
            }
        }
    }
}