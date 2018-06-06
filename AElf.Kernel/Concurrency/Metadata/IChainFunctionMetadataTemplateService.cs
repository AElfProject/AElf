using System;
using System.Collections.Generic;

namespace AElf.Kernel.Concurrency.Metadata
{
    
    public interface IChainFunctionMetadataTemplateService
    {
        /// <summary>
        /// use Map to store the function's metadata
        /// </summary>
        Dictionary<string, FunctionMetadataTemplate> FunctionMetadataTemplateMap { get; }

        bool TryAddNewContract(Type contractType);
    }
}