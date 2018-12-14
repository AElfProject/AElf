using System.Collections.Generic;
using AElf.Kernel;

namespace AElf.SmartContract.Consensus
{
    public interface IElectionInfo
    {
        bool IsCandidate(string publicKey);
        Tickets GetTicketsInfo(string publicKey);
        Round GetRoundNumber(ulong roundNumber);
        List<string> GetCurrentMines();
    }
}