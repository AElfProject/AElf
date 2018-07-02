﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Kernel.Types;

namespace AElf.Kernel.Concurrency.Metadata
{
    public interface IChainFunctionMetadata
    {
        /// <summary>
        /// Called when deploy a new contract
        /// TODO: need to be async when this access datastore
        /// </summary>
        /// <param name="contractClassName">Class name, should by reture value of Type.Name of the contract class</param>
        /// <param name="contractAddr">The address to be assigned to the contract</param>
        /// <param name="contractReferences">the map where smart contract member reference to its acutal address</param>
        /// <exception cref="InvalidOperationException">Throw when FunctionMetadataMap already contains a function with same fullname</exception>
        /// <returns>True when success, false when something is wrong (usually is cannot find record with respect to functionName in the parameter otherFunctionsCallByThis)</returns>
        Task DeployNewContract(string contractClassName, Hash contractAddr,
            Dictionary<string, Hash> contractReferences);
        
        /// <summary>
        /// Get a function's metadata, throw  if this function is not found in the map.
        /// TODO: need to be async when this access datastore
        /// </summary>
        /// <param name="functionFullName"></param>
        FunctionMetadata GetFunctionMetadata(string functionFullName);
        
        /// <summary>
        /// Update metadata of an existing function.
        /// This function should only be called when this contract legally update.
        /// </summary>
        /// <param name="functionFullName"></param>
        /// <param name="otherFunctionsCallByThis"></param>
        /// <param name="nonRecursivePathSet"></param>
        bool UpdataExistingMetadata(string functionFullName, HashSet<string> otherFunctionsCallByThis, HashSet<string> nonRecursivePathSet);

    }
}