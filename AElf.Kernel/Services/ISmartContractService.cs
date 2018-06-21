using System.Threading.Tasks;
using Google.Protobuf;

namespace AElf.Kernel.Services
{
    public interface ISmartContractService
    {
        Task<IExecutive> GetExecutiveAsync(Hash account, Hash chainId);
        Task PutExecutiveAsync(Hash account, IExecutive executive);
        Task DeployContractAsync(Hash account, SmartContractRegistration registration);
        Task<IMessage> GetAbiAsync(Hash account);
    }
}