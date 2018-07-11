namespace AElf.SmartContract
{
    public interface ISmartContractRunnerFactory
    {
        ISmartContractRunner GetRunner(int category);
        void AddRunner(int category, ISmartContractRunner runner);
        void UpdateRunner(int category, ISmartContractRunner runner);
    }
}