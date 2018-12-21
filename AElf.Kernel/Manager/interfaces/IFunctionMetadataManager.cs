using System.Threading.Tasks;
using AElf.Common;
using AElf.Kernel.SmartContract;

namespace AElf.Kernel.Manager.Interfaces
{
    public interface IFunctionMetadataManager
    {
        Task AddAsync(Hash chainId, string name, FunctionMetadata metadata);
        Task<FunctionMetadata> GetAsync(Hash chainId, string name);
        Task RemoveAsync(Hash chainId, string name);
    }
}