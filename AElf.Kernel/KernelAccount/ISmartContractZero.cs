using System.Threading.Tasks;

namespace AElf.Kernel.KernelAccount
{
    public interface ISmartContractZero : ISmartContract
    {
        Task RegisterSmartContract(SmartContractRegistration reg);
        Task DeploySmartContract(Hash account, SmartContractDeployment smartContractRegister);
        Task<ISmartContract> GetSmartContractAsync(Hash hash);
    }
}