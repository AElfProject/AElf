using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Kernel;

namespace AElf.SmartContract.Consensus
{
    public interface IElectionInfo
    {
        Task<bool> IsCandidate(int chainId, string publicKey);
        Task<Tickets> GetVotingInfo(int chainId, string publicKey);
        Task<Tuple<ulong, ulong>> GetVotesGeneral(int chainId);
        Task<Round> GetRoundInfo(int chainId, ulong roundNumber);
        Task<List<string>> GetCurrentMines(int chainId);
    }
}