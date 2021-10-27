namespace AElf.Kernel.SmartContract.Infrastructure
{
    public interface ISmartContractRunnerContainer
    {
        ISmartContractRunner GetRunner(int category);
        void AddRunner(int category, ISmartContractRunner runner);
        void UpdateRunner(int category, ISmartContractRunner runner);
    }
}