using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Kernel.Types;
using AElf.Common;

namespace AElf.SmartContract
{
    public interface IFunctionMetadataService
    {
        Task DeployContract(Hash chainId, Address address, ContractMetadataTemplate contractMetadataTemplate);
        Task<FunctionMetadata> GetFunctionMetadata(Hash chainId, string addrFunctionName);
    }
}