using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Kernel.Types;
using AElf.Common;
using AElf.Kernel.SmartContract;

namespace AElf.Kernel.SmartContract
{
    public interface IFunctionMetadataService
    {
        Task DeployContract(int chainId, Address address, ContractMetadataTemplate contractMetadataTemplate);
        Task UpdateContract(int chainId, Address address, ContractMetadataTemplate oldContractMetadataTemplate, ContractMetadataTemplate newContractMetadataTemplate);
        Task<FunctionMetadata> GetFunctionMetadata(int chainId, string addrFunctionName);
    }
}