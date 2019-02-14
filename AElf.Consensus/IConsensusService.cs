using System.Collections.Generic;
using AElf.Common;
using AElf.Kernel;

namespace AElf.Consensus
{
    public interface IConsensusService
    {
        bool ValidateConsensus(int chainId, Address fromAddress, byte[] consensusInformation);
        byte[] GetNewConsensusInformation(int chainId, Address fromAddress);
        IEnumerable<Transaction> GenerateConsensusTransactions(int chainId, Address fromAddress, ulong refBlockHeight, byte[] refBlockPrefix);
        byte[] GetConsensusCommand(int chainId, Address fromAddress);
    }
}