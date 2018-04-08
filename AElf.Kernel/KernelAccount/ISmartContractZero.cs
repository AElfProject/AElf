using System.Threading.Tasks;
using Google.Protobuf;

namespace AElf.Kernel.KernelAccount
{
    
    
    
    
    public interface ISmartContractZero : ISmartContract
    {
        Task RegisterSmartContract(SmartContractRegistration reg);
        Task DeploySmartContract(SmartContractDeployment smartContractRegister);
        Task<ISmartContract> GetSmartContractAsync(Hash hash);
    }
}