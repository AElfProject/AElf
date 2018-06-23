using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using QuickGraph;

namespace AElf.Kernel.Concurrency.Metadata
{
    
    public interface IChainFunctionMetadataTemplate
    {
        /// <summary>
        /// use Map to store the function's metadata
        /// </summary>
        Dictionary<string, Dictionary<string, FunctionMetadataTemplate>> ContractMetadataTemplateMap { get; }

        Task<bool> TryAddNewContract(Type contractType);

        bool TryGetLocalCallingGraph(Dictionary<string, FunctionMetadataTemplate> localFunctionMetadataTemplateMap,
            out AdjacencyGraph<string, Edge<string>> callGraph, out IEnumerable<string> topologicRes);
        
        Hash ChainId { get;}
    }
}