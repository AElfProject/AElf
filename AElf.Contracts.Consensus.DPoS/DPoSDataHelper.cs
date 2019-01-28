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
            throw new System.NotImplementedException();
        }

        public bool TryToUpdateTermNumber(ulong termNumber)
        {
            throw new System.NotImplementedException();
        }
        
        public bool TryToGetRoundNumber(out ulong roundNumber)
        {
            throw new System.NotImplementedException();
        }

        public bool TryToGetTermNumber(out ulong termNumber)
        {
            throw new System.NotImplementedException();
        }

        public bool TryToGetCurrentRoundInformation(out Round roundInformation)
        {
            throw new System.NotImplementedException();
        }

        public bool TryToGetPreviousRoundInformation(out Round roundInformation)
        {
            throw new System.NotImplementedException();
        }

        public bool TryToGetMiners(ulong termNumber, out Miners miners)
        {
            throw new System.NotImplementedException();
        }

        public bool TryToGetVictories(out Miners victories)
        {
            throw new System.NotImplementedException();
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

        public void SetTermNumber(ulong termNumber)
        {
            throw new System.NotImplementedException();
        }

        public void SetRoundNumber(ulong roundNumber)
        {
            throw new System.NotImplementedException();
        }

        public void SetBlockAge(ulong blockAge)
        {
            throw new System.NotImplementedException();
        }

        public void SetBlockchainStartTimestamp(Timestamp timestamp)
        {
            _dataStructures.BlockchainStartTimestamp.SetValue(timestamp);
        }

        public void AddOrUpdateMinerHistoryInformation(CandidateInHistory historyInformation)
        {
            throw new System.NotImplementedException();
        }

        public void AddRoundInformation(Round roundInformation)
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