using System.Collections;
using System.Collections.Generic;
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
        bool TryToGetRoundInformation(ulong roundNumber, out Round roundInformation);
        bool TryToGetMiners(ulong termNumber, out Miners miners);
        bool TryToGetVictories(out Miners victories);
        bool TryToGetMiningInterval(out int miningInterval);
        bool TryToGetCurrentAge(out ulong blockAge);
        bool TryToGetBlockchainStartTimestamp(out Timestamp timestamp);
        bool TryToGetMinerHistoryInformation(string publicKey, out CandidateInHistory historyInformation);
        bool TryToGetSnapshot(ulong termNumber, out TermSnapshot snapshot);
        bool TryToGetTicketsInformation(string publicKey, out Tickets tickets);
        bool TryToGetBackups(List<string> currentMiners, out List<string> backups);
        bool TryToGetChainId(out int chainId);

        void SetTermNumber(ulong termNumber);
        void SetRoundNumber(ulong roundNumber);
        void SetBlockAge(ulong blockAge);
        void SetBlockchainStartTimestamp(Timestamp timestamp);
        void AddOrUpdateMinerHistoryInformation(CandidateInHistory historyInformation);
        void AddOrUpdateTicketsInformation(Tickets tickets);
        void SetTermSnapshot(TermSnapshot snapshot);
        void SetAlias(string publicKey, string alias);
        void SetMiningInterval(int miningInterval);
        void SetChainId(int chainId);
        
        bool AddTermNumberToFirstRoundNumber(ulong termNumber, ulong firstRoundNumber);
        bool SetMiners(Miners miners, bool gonnaReplaceSomeone = false);
        bool SetSnapshot(TermSnapshot snapshot);
        bool TryToAddRoundInformation(Round roundInformation);
        bool TryToUpdateRoundInformation(Round roundInformation);
        
        bool IsMiner(Address address);
    }
}