using System.Threading.Tasks;

namespace AElf.Kernel.KernelAccount
{
    public interface ISmartContractZero : ISmartContract
    {
        Task RegisterSmartContract(SmartContractRegistration reg);
        Task<Hash> DeploySmartContract(SmartContractDeployment smartContractRegister);
        Task<ISmartContract> GetSmartContractAsync(Hash hash);
    }
}