namespace AElf.Kernel.Types.SmartContract
{
    public interface IConsensusSmartContract : ISmartContract
    {
        bool ValidateConsensus(byte[] consensusInformation);
        ulong GetCountingMilliseconds();
        byte[] GetNewConsensusInformation();
    }
}