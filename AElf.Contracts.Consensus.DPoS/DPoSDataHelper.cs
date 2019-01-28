using System.Collections;
using System.Collections.Generic;
using System.Linq;
using AElf.Common;
using AElf.Kernel;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Contracts.Consensus.DPoS
{
    // ReSharper disable once InconsistentNaming
    public class DPoSDataHelper : IDPoSDataHelper
    {
        private readonly DataStructures _dataStructures;

        public DPoSDataHelper(DataStructures dataStructures)
        {
            _dataStructures = dataStructures;
        }

        public bool TryToUpdateRoundNumber(ulong roundNumber)
        {
            var oldRoundNumber = _dataStructures.CurrentRoundNumberField.GetValue();
            if (roundNumber != 1 && oldRoundNumber + 1 != roundNumber)
            {
                return false;
            }
            
            _dataStructures.CurrentRoundNumberField.SetValue(roundNumber);
            return true;
        }

        public bool TryToUpdateTermNumber(ulong termNumber)
        {
            var oldTermNumber = _dataStructures.CurrentTermNumberField.GetValue();
            if (termNumber != 1 && oldTermNumber + 1 != termNumber)
            {
                return false;
            }
            
            _dataStructures.CurrentTermNumberField.SetValue(termNumber);
            return true;
        }
        
        public bool TryToGetRoundNumber(out ulong roundNumber)
        {
            roundNumber = _dataStructures.CurrentRoundNumberField.GetValue();
            return roundNumber != 0;
        }

        public bool TryToGetTermNumber(out ulong termNumber)
        {
            termNumber = _dataStructures.CurrentTermNumberField.GetValue();
            return termNumber != 0;
        }

        public bool TryToGetCurrentRoundInformation(out Round roundInformation)
        {
            throw new System.NotImplementedException();
        }

        public bool TryToGetPreviousRoundInformation(out Round roundInformation)
        {
            throw new System.NotImplementedException();
        }

        public bool TryToGetRoundInformation(ulong roundNumber, out Round roundInformation)
        {
            throw new System.NotImplementedException();
        }

        public bool TryToGetMiners(ulong termNumber, out Miners miners)
        {
            throw new System.NotImplementedException();
        }

        public bool TryToGetVictories(out Miners victories)
        {
            var candidates = _dataStructures.CandidatesField.GetValue();
            var ticketsMap = new Dictionary<string, ulong>();
            foreach (var candidatePublicKey in candidates.PublicKeys)
            {
                if (_dataStructures.TicketsMap.TryGet(candidatePublicKey.ToStringValue(), out var tickets))
                {
                    ticketsMap.Add(candidatePublicKey, tickets.ObtainedTickets);
                }
            }

            if (ticketsMap.Keys.Count < GlobalConfig.BlockProducerNumber)
            {
                victories = null;
                return false;
            }

            victories = ticketsMap.OrderByDescending(tm => tm.Value).Take(GlobalConfig.BlockProducerNumber)
                .Select(tm => tm.Key)
                .ToList().ToMiners();
            return true;
        }

        public bool TryToGetMiningInterval(out int miningInterval)
        {
            throw new System.NotImplementedException();
        }

        public bool TryToGetCurrentAge(out ulong blockAge)
        {
            throw new System.NotImplementedException();
        }

        public bool TryToGetBlockchainStartTimestamp(out Timestamp timestamp)
        {
            timestamp = _dataStructures.BlockchainStartTimestamp.GetValue();
            return timestamp != null;
        }

        public bool TryToGetMinerHistoryInformation(string publicKey, out CandidateInHistory historyInformation)
        {
            throw new System.NotImplementedException();
        }

        public bool TryToGetSnapshot(ulong termNumber, out TermSnapshot snapshot)
        {
            throw new System.NotImplementedException();
        }

        public bool TryToGetTicketsInformation(string publicKey, out Tickets tickets)
        {
            throw new System.NotImplementedException();
        }

        public bool TryToGetBackups(List<string> currentMiners, out List<string> backups)
        {
            var candidates = _dataStructures.CandidatesField.GetValue();
            backups = candidates.PublicKeys.Except(currentMiners).ToList();
            return backups.Any();
        }

        public void SetTermNumber(ulong termNumber)
        {
            _dataStructures.CurrentTermNumberField.SetValue(termNumber);
        }

        public void SetRoundNumber(ulong roundNumber)
        {
            _dataStructures.CurrentRoundNumberField.SetValue(roundNumber);
        }

        public void SetBlockAge(ulong blockAge)
        {
            _dataStructures.AgeField.SetValue(blockAge);
        }

        public void SetBlockchainStartTimestamp(Timestamp timestamp)
        {
            _dataStructures.BlockchainStartTimestamp.SetValue(timestamp);
        }

        public void AddOrUpdateMinerHistoryInformation(CandidateInHistory historyInformation)
        {
            _dataStructures.HistoryMap.SetValue(historyInformation.PublicKey.ToStringValue(), historyInformation);
        }

        public bool TryToAddRoundInformation(Round roundInformation)
        {
            if (_dataStructures.RoundsMap.TryGet(roundInformation.RoundNumber.ToUInt64Value(), out _))
            {
                return false;
            }
            
            _dataStructures.RoundsMap.SetValue(roundInformation.RoundNumber.ToUInt64Value(), roundInformation);
            return true;
        }

        public bool TryToUpdateRoundInformation(Round roundInformation)
        {
            if (!_dataStructures.RoundsMap.TryGet(roundInformation.RoundNumber.ToUInt64Value(), out _))
            {
                return false;
            }
            
            _dataStructures.RoundsMap.SetValue(roundInformation.RoundNumber.ToUInt64Value(), roundInformation);
            return true;
        }

        public void AddOrUpdateTicketsInformation(Tickets tickets)
        {
            throw new System.NotImplementedException();
        }

        public void SetTermSnapshot(TermSnapshot snapshot)
        {
            throw new System.NotImplementedException();
        }

        public void SetAlias(string publicKey, string alias)
        {
            throw new System.NotImplementedException();
        }

        public bool AddTermNumberToFirstRoundNumber(ulong termNumber, ulong firstRoundNumber)
        {
            // Need a new Map.
            throw new System.NotImplementedException();
        }

        public bool SetMiners(Miners miners, bool gonnaReplaceSomeone = false)
        {
            // Miners for one specific term should only update once.
            if (gonnaReplaceSomeone || !_dataStructures.MinersMap.TryGet(miners.TermNumber.ToUInt64Value(), out _))
            {
                _dataStructures.MinersMap.SetValue(miners.TermNumber.ToUInt64Value(), miners);
                return true;
            }

            return false;
        }

        public bool SetSnapshot(TermSnapshot snapshot)
        {
            throw new System.NotImplementedException();
        }

        public bool IsMiner(Address address)
        {
            if (TryToGetTermNumber(out var termNumber))
            {
                if (TryToGetMiners(termNumber, out var miners))
                {
                    return miners.Addresses.Contains(address);
                }
            }

            return false;
        }
    }
}