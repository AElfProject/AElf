using System.Threading.Tasks;

namespace AElf.Kernel.KernelAccount
{
    public interface ISmartContractZero : ISmartContract
    {
        Task<object> RegisterSmartContract(SmartContractRegistration reg);
        Task<object> DeploySmartContract(SmartContractDeployment smartContractRegister);
        Task<ISmartContract> GetSmartContractAsync(Hash hash);
    }
}