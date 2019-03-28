using AElf.Kernel;

namespace AElf.Sdk.CSharp
{
    public interface IConsensusSmartContract : ISmartContract
    {
        ValidationResult ValidateConsensusBeforeExecution(byte[] consensusInformation);
        ValidationResult ValidateConsensusAfterExecution(byte[] consensusInformation);
        byte[] GetNewConsensusInformation(byte[] consensusTriggerInformation);
        TransactionList GenerateConsensusTransactions(byte[] extraInformation);
        byte[] GetConsensusCommand(byte[] consensusTriggerInformation);
    }
}