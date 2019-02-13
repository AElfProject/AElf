using AElf.Common;
using AElf.Kernel;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Consensus
{
    public interface IConsensusService
    {
        bool ValidateConsensus(int chainId, Address fromAddress, byte[] consensusInformation);
        int GetCountingMilliseconds(int chainId, Address fromAddress);
        byte[] GetNewConsensusInformation(int chainId, Address fromAddress);
        TransactionList GenerateConsensusTransactions(int chainId, Address fromAddress, ulong currentBlockHeight, Hash previousBlockHash);
    }
}