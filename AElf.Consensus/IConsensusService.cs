using AElf.Common;
using AElf.Kernel;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Consensus
{
    public interface IConsensusService
    {
        ValidationResult ValidateConsensus(byte[] consensusInformation);
        int GetCountingMilliseconds(Timestamp timestamp);
        IMessage GetNewConsensusInformation();
        TransactionList GenerateConsensusTransactions(ulong currentBlockHeight, Hash previousBlockHash);
    }
}