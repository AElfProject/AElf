using Google.Protobuf.WellKnownTypes;

namespace AElf.Kernel.Types.SmartContract
{
    public interface IConsensusSmartContract : ISmartContract
    {
        ValidationResult ValidateConsensus(byte[] consensusInformation);
        ulong GetCountingMilliseconds(Timestamp timestamp);
        byte[] GetNewConsensusInformation();
        TransactionList GenerateConsensusTransactions();
    }
}