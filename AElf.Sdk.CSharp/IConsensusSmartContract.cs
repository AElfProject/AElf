using AElf.Common;
using AElf.Kernel;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Sdk.CSharp
{
    public interface IConsensusSmartContract : ISmartContract
    {
        ValidationResult ValidateConsensus(byte[] consensusInformation);
        byte[] GetNewConsensusInformation(byte[] consensusTriggerInformation);
        TransactionList GenerateConsensusTransactions(byte[] extraInformation);
        byte[] GetConsensusCommand(byte[] consensusTriggerInformation);
    }
}