using System;
using System.Collections.Generic;
using System.Linq;
using NLog;
using Org.BouncyCastle.Security;

namespace AElf.Kernel.Concurrency.Metadata
{
    public class ChainFunctionMetadataService : IChainFunctionMetadataService
    {
        private readonly IChainFunctionMetadataTemplateService _templateService;
        private readonly ILogger _logger;
        
        public Dictionary<string, FunctionMetadata> FunctionMetadataMap { get; } = new Dictionary<string, FunctionMetadata>();
        
        public ChainFunctionMetadataService(IChainFunctionMetadataTemplateService templateService,
            ILogger logger = null)
        {
            _templateService = templateService;
            _logger = logger;
        }

        public bool SetNewFunctionMetadata(string functionFullName, HashSet<string> otherFunctionsCallByThis, HashSet<string> nonRecursivePathSet)
        {
            if (FunctionMetadataMap.ContainsKey(functionFullName))
            {
                //This should be the completely new function
                throw new InvalidOperationException("FunctionMetadataMap already contain a function named " + functionFullName);
            }

            HashSet<string> resourceSet = new HashSet<string>(nonRecursivePathSet);

            try
            {
                //Any metadata that already in the FunctionMetadataMap are already recursively process and set, so we just union their path set.
                foreach (var calledFunc in otherFunctionsCallByThis ?? Enumerable.Empty<string>())
                {
                    var metadataOfCalledFunc = GetFunctionMetadata(calledFunc);
                    resourceSet.UnionWith(metadataOfCalledFunc.FullResourceSet);
                }
            }
            catch (InvalidParameterException e)
            {
                _logger?.Error(e, "when tries to add function: " + functionFullName + ", it cause non-DAG calling graph thus fail.");
                return false;
            }
            
            var metadata = new FunctionMetadata(otherFunctionsCallByThis, resourceSet, nonRecursivePathSet);
            
            FunctionMetadataMap.Add(functionFullName, metadata);

            return true;
        }

        public FunctionMetadata GetFunctionMetadata(string functionFullName)
        {
            if (FunctionMetadataMap.TryGetValue(functionFullName, out var txMetadata))
            {
                return txMetadata;
            }
            else
            {
                throw new InvalidParameterException("There are no function named " + functionFullName +
                                                    " in the FunctionMetadataMap");
            }
        }
        
        
        public bool UpdataExistingMetadata(string functionFullName, HashSet<string> newOtherFunctionsCallByThis, HashSet<string> newNonRecursivePathSet)
        {
            if (!FunctionMetadataMap.ContainsKey(functionFullName))
            {
                throw new InvalidOperationException("FunctionMetadataMap don't contain a function named " + functionFullName + " when trying to update this function's metadata");
            }
            
            var oldMetadata = FunctionMetadataMap[functionFullName];

            FunctionMetadataMap.Remove(functionFullName);
            
            if(!SetNewFunctionMetadata(functionFullName, newOtherFunctionsCallByThis, newNonRecursivePathSet)){
                //This should be unReachable, because function above already check whether new graph is DAG
                FunctionMetadataMap.Add(functionFullName, oldMetadata);
                return false;
            }

            UpdateInfluencedMetadata(functionFullName);

            return true;
        }

        /// <summary>
        /// Update other functions that call the updated function (backward recursively).
        /// </summary>
        /// <param name="updatedFunctionFullName"></param>
        /// <exception cref="NotImplementedException"></exception>
        private void UpdateInfluencedMetadata(string updatedFunctionFullName)
        {
            if(TryFindCallerFunctions(updatedFunctionFullName, out var callerFuncs))
            {
                foreach (var caller in callerFuncs)
                {
                    var oldMetadata = FunctionMetadataMap[caller];
                    FunctionMetadataMap.Remove(caller);
                    SetNewFunctionMetadata(caller, oldMetadata.CallingSet, oldMetadata.LocalResourceSet);
                    UpdateInfluencedMetadata(caller);
                }
            }
        }

        /// <summary>
        /// Find the functions in the calling graph that call this func
        /// </summary>
        /// <param name="calledFunctionFullName">Full name of the called function</param>
        /// <param name="callerFunctions">result</param>
        /// <returns>True if find any</returns>
        private bool TryFindCallerFunctions(string calledFunctionFullName, out List<string> callerFunctions)
        {
            callerFunctions = FunctionMetadataMap.Where(funcMeta => funcMeta.Value.CallingSet.Contains(calledFunctionFullName))
                .Select(a => a.Key).ToList();

            return callerFunctions.Count != 0;
        }
    }
}