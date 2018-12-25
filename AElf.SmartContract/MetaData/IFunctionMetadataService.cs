using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Kernel.Types;
using AElf.Common;
using AElf.Kernel.SmartContract;

namespace AElf.SmartContract
{
    public interface IFunctionMetadataService
    {
        Task DeployContract(Hash chainId, Address address, ContractMetadataTemplate contractMetadataTemplate);
        Task UpdateContract(Hash chainId, Address address, ContractMetadataTemplate oldContractMetadataTemplate, ContractMetadataTemplate newContractMetadataTemplate);
        Task<FunctionMetadata> GetFunctionMetadata(Hash chainId, string addrFunctionName);
    }
}