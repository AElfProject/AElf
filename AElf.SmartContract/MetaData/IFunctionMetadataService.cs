using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Kernel.Types;
using AElf.Kernel;

namespace AElf.SmartContract
{
    public interface IFunctionMetadataService
    {
        Task DeployContract(Hash chainId, Type contractType, Hash address, Dictionary<string, Hash> contractReferences);
        FunctionMetadata GetFunctionMetadata(Hash chainId, string addrFunctionName);
    }
}