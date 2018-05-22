using System;
using System.Collections.Generic;
using System.Linq;
using Org.BouncyCastle.Security;
using ServiceStack;

namespace AElf.Kernel.Concurrency
{
    public class FunctionMetadataService : IFunctionMetaDataService
    {
        public Dictionary<string, FunctionMetadata> FunctionMetadataMap { get; } = new Dictionary<string, FunctionMetadata>();
        
        
        public bool SetNewFunctionMetadata(string functionFullName, HashSet<string> otherFunctionsCallByThis, HashSet<Hash> nonRecursivePathSet)
        {
            if (FunctionMetadataMap.ContainsKey(functionFullName))
            {
                throw new InvalidOperationException("FunctionMetadataService: FunctionMetadataMap already contain a function named " + functionFullName);
            }

            HashSet<Hash> pathSet = new HashSet<Hash>(nonRecursivePathSet);

            try
            {
                //Any metadata that already in the FunctionMetadataMap are already recursively process and set, so we just union their path set.
                foreach (var calledFunc in otherFunctionsCallByThis ?? Enumerable.Empty<string>())
                {
                    var metadataOfCalledFunc = GetFunctionMetadata(calledFunc);
                    pathSet.UnionWith(metadataOfCalledFunc.PathSet);
                }
            }
            catch (InvalidParameterException e)
            {
                Console.WriteLine(e);
                return false;
            }
            
            var metadata = new FunctionMetadata(otherFunctionsCallByThis, pathSet);
            
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
                throw new InvalidParameterException("FunctionMetadataService: There are no function named " + functionFullName +
                                                    " in the FunctionMetadataMap");
            }
        }

        public bool UpdataExistingMetadata(string functionFullName, HashSet<string> newOtherFunctionsCallByThis, HashSet<Hash> newNonRecursivePathSet)
        {
            if (!FunctionMetadataMap.ContainsKey(functionFullName))
            {
                throw new InvalidOperationException("FunctionMetadataService: FunctionMetadataMap don't contain a function named " + functionFullName + " when trying to update this function's metadata");
            }

            var oldMetadata = FunctionMetadataMap[functionFullName];

            FunctionMetadataMap.Remove(functionFullName);
            
            if(!SetNewFunctionMetadata(functionFullName, newOtherFunctionsCallByThis, newNonRecursivePathSet)){
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
                    SetNewFunctionMetadata(caller, oldMetadata.CallingSet, oldMetadata.NonRecursivePathSet);
                    UpdateInfluencedMetadata(caller);
                }
            }
        }

        private bool TryFindCallerFunctions(string calledFunctionFullName, out List<string> callerFunctions)
        {
            callerFunctions = FunctionMetadataMap.Where(funcMeta => funcMeta.Value.CallingSet.Contains(calledFunctionFullName))
                .Select(a => a.Key).ToList();
            
            return !callerFunctions.IsEmpty();
        }
    }
}