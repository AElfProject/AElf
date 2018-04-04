namespace AElf.Kernel.KernelAccount
{
    public interface ISmartContractRunnerFactory
    {
        ISmartContractRunner GetRunner(int category);
    }
}