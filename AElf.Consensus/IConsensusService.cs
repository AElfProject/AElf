using System.Collections.Generic;
using AElf.Common;
using AElf.Kernel;

namespace AElf.Consensus
{
    public interface IConsensusService
    {
        bool ValidateConsensus(int chainId, Address fromAddress, byte[] consensusInformation);
        int GetCountingMilliseconds(int chainId, Address fromAddress);
        byte[] GetNewConsensusInformation(int chainId, Address fromAddress);
        List<Transaction> GenerateConsensusTransactions(int chainId, Address fromAddress, ulong refBlockHeight, byte[] refBlockPrefix);
    }
}