using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Kernel.Types;

namespace AElf.Kernel.Concurrency.Metadata
{
    public interface IFunctionMetadataService
    {
        Task DeployContract(Hash chainId, Type contractType, Hash address, Dictionary<string, Hash> contractReferences);
        FunctionMetadata GetFunctionMetadata(Hash chainId, string addrFunctionName);
        ConcurrentDictionary<Hash, ChainFunctionMetadata> Metadatas { get; }
    }
}