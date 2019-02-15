namespace AElf.Kernel.SmartContract
{
    public interface ISmartContractRunnerContainer
    {
        ISmartContractRunner GetRunner(int category);
        void AddRunner(int category, ISmartContractRunner runner);
        void UpdateRunner(int category, ISmartContractRunner runner);
    }
}