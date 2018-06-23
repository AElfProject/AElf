using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AElf.Kernel.Concurrency.Metadata
{
    public interface IFunctionMetadataService
    {
        Task DeployContract(Hash chainId, Type contractType, Hash address, Dictionary<string, Hash> contractReferences);
        Task<FunctionMetadata> GetFunctionMetadata(Hash chainId, string addrFunctionName);
    }
}