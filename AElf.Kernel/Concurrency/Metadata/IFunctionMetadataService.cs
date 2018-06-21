using System;
using System.Threading.Tasks;

namespace AElf.Kernel.Concurrency.Metadata
{
    public interface IFunctionMetadataService
    {
        Task DeployContract(Hash chainId, Type contractType, Hash address);
        Task<FunctionMetadata> GetFunctionMetadata(Hash chainId, string addrFunctionName);
    }
}