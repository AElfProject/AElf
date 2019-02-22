using AElf.Common;

namespace AElf.Kernel.SmartContract.Application
{
    public interface IDefaultContractZeroCodeProvider
    {
        SmartContractRegistration DefaultContractZeroRegistration { get; set; }
    }

    public class DefaultContractZeroCodeProvider : IDefaultContractZeroCodeProvider
    {
        public SmartContractRegistration DefaultContractZeroRegistration { get; set; }
    }
}