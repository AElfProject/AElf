using System;
using System.Collections.Generic;
using System.Linq;
using NLog;
using Org.BouncyCastle.Security;
using ServiceStack;

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

        /// <summary>
        /// 
        /// </summary>
        /// <param name="functionFullName">should be "[Addr].FunctionSig"</param>
        /// <param name="contractAddr"></param>
        /// <param name="contractReferences"></param>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException"></exception>
        /// <exception cref="FunctionMetadataException"></exception>
        public bool DeployNewFunction(string functionFullName, Hash contractAddr, Dictionary<string, Hash> contractReferences)
        {
            if (FunctionMetadataMap.ContainsKey(functionFullName))
            {
                //This should be the completely new function
                throw new InvalidOperationException("FunctionMetadataMap already contain a function named " + functionFullName);
            }

            if (!_templateService.FunctionMetadataTemplateMap.TryGetValue(functionFullName, out var metadataTemplate))
            {
                throw new InvalidOperationException("No function named " + functionFullName + " in the metadata template map");
            }

            var resourceSet = metadataTemplate.LocalResourceSet.Select(resource =>
                {
                    var resName = Replacement.ReplaceValueIntoReplacement(resource.Name, Replacement.This, contractAddr.ToString());
                    return new Resource(resName, resource.DataAccessMode);
                }).ToHashSet();
            
            var localResourceSet = new HashSet<Resource>(resourceSet);
            var callingSet = new HashSet<string>();
            
            foreach (var calledFunc in metadataTemplate.CallingSet ?? Enumerable.Empty<string>())
            {
                if (! Replacement.TryGetReplacementWithIndex(calledFunc, 0, out var locationReplacement))
                {
                    throw new FunctionMetadataException("not valid template in calling set of function " +
                                                        functionFullName + " because the calling function" +
                                                        calledFunc +
                                                        "have no location replacement (${this} or ${[calling contract name]})");
                }

                //just add foreign resource into set because local resources are already recursively analyzed
                if (!locationReplacement.Equals(Replacement.This))
                {
                    Replacement.TryGetReplacementWithIndex(calledFunc, 0, out var memberReplacement);
                    var replacedCalledFunc = Replacement.ReplaceValueIntoReplacement(calledFunc, memberReplacement,
                        contractReferences[Replacement.Value(memberReplacement)].ToString());
                    
                    var metadataOfCalledFunc = GetFunctionMetadata(replacedCalledFunc); //could throw exception
                    
                    resourceSet.UnionWith(metadataOfCalledFunc.FullResourceSet);
                    callingSet.Add(replacedCalledFunc);
                }
                //TODO: do we still need local function that called recorded in the calling set?
            }
            
            var metadata = new FunctionMetadata(callingSet, resourceSet, localResourceSet);
            
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
            throw new NotImplementedException();
            /*
            if (!FunctionMetadataMap.ContainsKey(functionFullName))
            {
                throw new InvalidOperationException("FunctionMetadataMap don't contain a function named " + functionFullName + " when trying to update this function's metadata");
            }
            
            var oldMetadata = FunctionMetadataMap[functionFullName];

            FunctionMetadataMap.Remove(functionFullName);
            
            if(!DeployNewFunction(functionFullName, newOtherFunctionsCallByThis, newNonRecursivePathSet)){
                //This should be unReachable, because function above already check whether new graph is DAG
                FunctionMetadataMap.Add(functionFullName, oldMetadata);
                return false;
            }

            UpdateInfluencedMetadata(functionFullName);

            return true;
            */
        }

        /// <summary>
        /// Update other functions that call the updated function (backward recursively).
        /// </summary> 
        /// <param name="updatedFunctionFullName"></param>
        /// <exception cref="NotImplementedException"></exception>
        private void UpdateInfluencedMetadata(string updatedFunctionFullName)
        {
            throw new NotImplementedException();
            /*
            if(TryFindCallerFunctions(updatedFunctionFullName, out var callerFuncs))
            {
                foreach (var caller in callerFuncs)
                {
                    var oldMetadata = FunctionMetadataMap[caller];
                    FunctionMetadataMap.Remove(caller);
                    DeployNewFunction(caller, oldMetadata.CallingSet, oldMetadata.LocalResourceSet);
                    UpdateInfluencedMetadata(caller);
                }
            }
            */
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