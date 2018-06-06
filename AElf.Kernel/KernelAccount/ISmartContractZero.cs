using System.Threading.Tasks;

namespace AElf.Kernel.KernelAccount
{
    public interface ISmartContractZero : ISmartContract
    {
        Task<object> RegisterSmartContractAsync(SmartContractRegistration reg);
        Task<object> DeploySmartContractAsync(SmartContractDeployment smartContractRegister);
        Task<ISmartContract> GetSmartContractAsync(Hash hash);
    }
}