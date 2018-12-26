using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Kernel;

namespace AElf.SmartContract.Consensus
{
    public interface IElectionInfo
    {
        Task<bool> IsCandidate(string publicKey);
        Task<Tickets> GetVotingInfo(string publicKey);
        Task<Tuple<ulong, ulong>> GetVotesGeneral();
        Task<Round> GetRoundInfo(ulong roundNumber);
        Task<List<string>> GetCurrentMines();
    }
}