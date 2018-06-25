using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Kernel.Storages;
using AElf.Kernel.Types;
using Google.Protobuf;
using NLog;
using Org.BouncyCastle.Security;
using QuickGraph;

namespace AElf.Kernel.Concurrency.Metadata
{
    public class ChainFunctionMetadata : IChainFunctionMetadata
    {
        public readonly IChainFunctionMetadataTemplate Template;
        private readonly ILogger _logger;
        private readonly IDataStore _dataStore;
        private Hash ChainId => Template.ChainId;
        
        public Dictionary<string, FunctionMetadata> FunctionMetadataMap { get; }
        
        public ChainFunctionMetadata(IChainFunctionMetadataTemplate template, IDataStore dataStore,  ILogger logger)
        {
            Template = template;
            _dataStore = dataStore;
            _logger = logger;

            var mapCache = _dataStore.GetDataAsync(Path.CalculatePointerForMetadata(ChainId)).Result;
            if (mapCache != null)
            {
                FunctionMetadataMap = RestoreFunctionMetadata(SerializeFunctionMetadataMap.Parser.ParseFrom(mapCache));
            }
            else
            {
                FunctionMetadataMap = new Dictionary<string, FunctionMetadata>();
            }
        }

        /// <summary>
        /// //TODO: in fact, only public interface of contact need to be added into FunctionMetadataMap
        /// </summary>
        /// <param name="contractClassName"></param>
        /// <param name="contractAddr"></param>
        /// <param name="contractReferences"></param>
        /// <exception cref="FunctionMetadataException"></exception>
        public async Task DeployNewContract(string contractClassName, Hash contractAddr, Dictionary<string, Hash> contractReferences)
        {
            Dictionary<string, FunctionMetadata> tempMap = new Dictionary<string, FunctionMetadata>();
            try
            {
                if (!Template.ContractMetadataTemplateMap.TryGetValue(contractClassName, out var classTemplate))
                {
                    throw new FunctionMetadataException("Cannot find contract named " + contractClassName + " in the template storage");
                }

                //local calling graph in template map of template must be topological, so ignore the callGraph
                Template.TryGetLocalCallingGraph(classTemplate, out var callGraph, out var topologicRes);

                foreach (var localFuncName in topologicRes.Reverse())
                {
                    var funcNameWithAddr =
                        Replacement.ReplaceValueIntoReplacement(localFuncName, Replacement.This, contractAddr.Value.ToBase64());
                    var funcMetadata = GetMetadataForNewFunction(funcNameWithAddr, classTemplate[localFuncName], contractAddr, contractReferences, tempMap);
                
                    tempMap.Add(funcNameWithAddr, funcMetadata);
                }
            
                //if no exception is thrown, merge the tempMap into FunctionMetadataMap
                foreach (var functionMetadata in tempMap)
                {
                    FunctionMetadataMap.Add(functionMetadata.Key, functionMetadata.Value);
                }

                await _dataStore.SetDataAsync(Path.CalculatePointerForMetadata(ChainId),
                    GenerateSerializeFunctionMetadataMap().ToByteArray());
            }
            catch (FunctionMetadataException e)
            {
                _logger?.Error(e, e.Message);
                throw;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="functionFullName">should be "[Addr].FunctionSig"</param>
        /// <param name="functionTemplate"></param>
        /// <param name="contractAddr"></param>
        /// <param name="contractReferences"></param>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException"></exception>
        /// <exception cref="FunctionMetadataException"></exception>
        private FunctionMetadata GetMetadataForNewFunction(string functionFullName, FunctionMetadataTemplate functionTemplate, Hash contractAddr, Dictionary<string, Hash> contractReferences, Dictionary<string, FunctionMetadata> localMetadataMap)
        {
            var resourceSet = new HashSet<Resource>(functionTemplate.LocalResourceSet.Select(resource =>
                {
                    var resName = Replacement.ReplaceValueIntoReplacement(resource.Name, Replacement.This, contractAddr.Value.ToBase64());
                    return new Resource(resName, resource.DataAccessMode);
                }));
            
            var localResourceSet = new HashSet<Resource>(resourceSet);
            var callingSet = new HashSet<string>();
            
            foreach (var calledFunc in functionTemplate.CallingSet ?? Enumerable.Empty<string>())
            {
                if (! Replacement.TryGetReplacementWithIndex(calledFunc, 0, out var locationReplacement))
                {
                    throw new FunctionMetadataException("not valid template in calling set of function " +
                                                        functionFullName + " because the calling function" +
                                                        calledFunc +
                                                        "have no location replacement (${this} or ${[calling contract name]})");
                }

                //just add foreign resource into set because local resources are already recursively analyzed
                if (locationReplacement.Equals(Replacement.This))
                {
                    var replacedCalledFunc = Replacement.ReplaceValueIntoReplacement(calledFunc, Replacement.This,
                        contractAddr.Value.ToBase64());
                    if (!localMetadataMap.TryGetValue(replacedCalledFunc, out var localCalledFuncMetadata))
                    {
                        throw new FunctionMetadataException("There are no local function " + replacedCalledFunc + " in the given local function map, consider wrong reference cause wrong topological order");
                    }
                    resourceSet.UnionWith(localCalledFuncMetadata.FullResourceSet);
                    callingSet.Add(replacedCalledFunc);
                }
                else 
                {
                    if (!contractReferences.TryGetValue(Replacement.Value(locationReplacement), out var referenceAddr))
                    {
                        throw new FunctionMetadataException("There are no member reference " + Replacement.Value(locationReplacement) + " in the given contractReferences map");
                    }
                    var replacedCalledFunc = Replacement.ReplaceValueIntoReplacement(calledFunc, locationReplacement,
                        referenceAddr.Value.ToBase64());
                    
                    var metadataOfCalledFunc = GetFunctionMetadata(replacedCalledFunc); //could throw exception
                    
                    resourceSet.UnionWith(metadataOfCalledFunc.FullResourceSet);
                    callingSet.Add(replacedCalledFunc);
                }
            }
            
            var metadata = new FunctionMetadata(callingSet, resourceSet, localResourceSet);

            return metadata;
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

        #region Serialize

        public SerializeFunctionMetadataMap GenerateSerializeFunctionMetadataMap()
        {
            var serializeMap = new SerializeFunctionMetadataMap();
            foreach (var metadata in FunctionMetadataMap)
            {
                serializeMap.MetadataMap.Add(metadata.Key, metadata.Value);
            }

            return serializeMap;
        }

        public Dictionary<string, FunctionMetadata> RestoreFunctionMetadata(SerializeFunctionMetadataMap serializeMap)
        {
            var metadataMap = new Dictionary<string, FunctionMetadata>();
            foreach (var kv in serializeMap.MetadataMap)
            {
                metadataMap.Add(kv.Key, kv.Value);
            }

            return metadataMap;
        }
        

        #endregion
    }
}