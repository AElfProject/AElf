using System;
using System.Collections.Generic;
using AElf.Kernel;

namespace AElf.SmartContract.Consensus
{
    public interface IElectionInfo
    {
        bool IsCandidate(string publicKey);
        Tickets GetVotingInfo(string publicKey);
        Tuple<ulong, ulong> GetVotesGeneral();
        Round GetRoundInfo(ulong roundNumber);
        List<string> GetCurrentMines();
    }
}