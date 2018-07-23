using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Kernel.Types;
using AElf.Kernel;

namespace AElf.SmartContract
{
    public interface IFunctionMetadataService
    {
        Task DeployContract(Hash chainId, Hash address, ContractMetadataTemplate contractMetadataTemplate);
        Task<FunctionMetadata> GetFunctionMetadata(Hash chainId, string addrFunctionName);
    }
}