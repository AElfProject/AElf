using AElf.Common;
using AElf.Kernel;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Sdk.CSharp
{
    public interface IConsensusSmartContract : ISmartContract
    {
        ValidationResult ValidateConsensus(byte[] consensusInformation);
        IMessage GetNewConsensusInformation(byte[] requestConsensusExtraData);

        TransactionList GenerateConsensusTransactions(byte[] extraInformation);
        IMessage GetConsensusCommand(byte[] consensusTriggerInformation);
    }
}