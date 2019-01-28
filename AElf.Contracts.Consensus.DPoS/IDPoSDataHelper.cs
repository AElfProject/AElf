using AElf.Common;
using AElf.Kernel;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Contracts.Consensus.DPoS
{
    // ReSharper disable once InconsistentNaming
    public interface IDPoSDataHelper
    {
        bool TryToUpdateRoundNumber(ulong roundNumber);
        bool TryToUpdateTermNumber(ulong termNumber);
        
        bool TryToGetRoundNumber(out ulong roundNumber);
        bool TryToGetTermNumber(out ulong termNumber);
        bool TryToGetCurrentRoundInformation(out Round roundInformation);
        bool TryToGetPreviousRoundInformation(out Round roundInformation);
        bool TryToGetMiners(ulong termNumber, out Miners miners);
        bool TryToGetVictories(out Miners victories);
        bool TryToGetMiningInterval(out int miningInterval);
        bool TryToGetCurrentAge(out ulong blockAge);
        bool TryToGetBlockchainStartTimestamp(out Timestamp timestamp);
        bool TryToGetMinerHistoryInformation(string publicKey, out CandidateInHistory historyInformation);

        void SetTermNumber(ulong termNumber);
        void SetRoundNumber(ulong roundNumber);
        void SetBlockAge(ulong blockAge);
        void SetBlockchainStartTimestamp(Timestamp timestamp);
        void AddOrUpdateMinerHistoryInformation(CandidateInHistory historyInformation);
        void AddRoundInformation(Round roundInformation);
        
        bool AddTermNumberToFirstRoundNumber(ulong termNumber, ulong firstRoundNumber);
        bool SetMiners(Miners miners, bool gonnaReplaceSomeone = false);

        bool IsMiner(Address address);
    }
}