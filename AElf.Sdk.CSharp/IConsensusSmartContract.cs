using AElf.Kernel;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Sdk.CSharp
{
    public interface IConsensusSmartContract : ISmartContract
    {
        ValidationResult ValidateConsensus(byte[] consensusInformation);
        int GetCountingMilliseconds(Timestamp timestamp);
        byte[] GetNewConsensusInformation(byte[] extraInformation);
        TransactionList GenerateConsensusTransactions(BlockHeader blockHeader, byte[] extraInformation);
    }
}