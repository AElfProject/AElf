using AElf.Kernel.KernelAccount;

namespace AElf.Kernel
{
    public interface IGenesisBlockBuilder
    {
        IGenesisBlock Build(ISmartContractZero smartContractZero,
            SmartContractRegistration smartContractRegistration);
    }
}