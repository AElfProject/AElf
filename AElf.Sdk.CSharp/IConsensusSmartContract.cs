using AElf.Kernel;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Sdk.CSharp
{
    public interface IConsensusSmartContract : ISmartContract
    {
        ValidationResult ValidateConsensus(byte[] consensusInformation);
        int GetCountingMilliseconds(Timestamp timestamp);
        IMessage GetNewConsensusInformation(byte[] extraInformation);
        TransactionList GenerateConsensusTransactions(BlockHeader previousBlockHeader, byte[] extraInformation);
    }
}