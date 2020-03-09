using System.Threading.Tasks;
using AElf.Types;
using Google.Protobuf;

namespace AElf.Kernel.SmartContract.Infrastructure
{
    public interface ISmartContractRunner : ISmartContractCategoryProvider
    {
        Task<IExecutive> RunAsync(SmartContractRegistration reg);
    }
}