using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Org.BouncyCastle.Security;
using QuickGraph;
using QuickGraph.Algorithms;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using AElf.Kernel.SmartContract.Domain;
using AElf.Kernel.SmartContract.MetaData;

namespace AElf.Kernel.SmartContract
{
    public class ChainFunctionMetadata : IChainFunctionMetadata
    {
        public ILogger<ChainFunctionMetadata> Logger { get; set; }
        private readonly IFunctionMetadataManager _functionMetadataManager;

        public Dictionary<string, FunctionMetadata> FunctionMetadataMap = new Dictionary<string, FunctionMetadata>();


        public ChainFunctionMetadata(IFunctionMetadataManager functionMetadataManager)
        {
            Logger = NullLogger<ChainFunctionMetadata>.Instance;
            _functionMetadataManager = functionMetadataManager;
        }

        /// <summary>
        /// //TODO: in fact, only public interface of contact need to be added into FunctionMetadataMap
        /// </summary>
        /// <param name="contractAddr"></param>
        /// <param name="contractMetadataTemplate"></param>
        /// <exception cref="FunctionMetadataException"></exception>
        public async Task DeployNewContract(Address contractAddr, ContractMetadataTemplate contractMetadataTemplate)
        {
            Dictionary<string, FunctionMetadata> tempMap = new Dictionary<string, FunctionMetadata>();
            try
            {
                var globalCallGraph = await GetCallingGraphForChain();
                var newCallGraph = TryUpdateAndGetCallingGraph(contractAddr, globalCallGraph, contractMetadataTemplate);

                foreach (var localFuncName in contractMetadataTemplate.ProcessFunctionOrder)
                {
                    var funcNameWithAddr =
                        Replacement.ReplaceValueIntoReplacement(localFuncName, Replacement.This,
                            contractAddr.GetFormatted());
                    var funcMetadata = await GetMetadataForNewFunction(funcNameWithAddr,
                        contractMetadataTemplate.MethodMetadataTemplates[localFuncName],
                        contractAddr, contractMetadataTemplate.ContractReferences, tempMap);

                    tempMap.Add(funcNameWithAddr, funcMetadata);
                }

                //if no exception is thrown, merge the tempMap into FunctionMetadataMap and update call graph in database
                await _functionMetadataManager.AddCallGraphAsync(SerializeCallingGraph(newCallGraph));

                foreach (var functionMetadata in tempMap)
                {
                    FunctionMetadataMap.Add(functionMetadata.Key, functionMetadata.Value);
                    await _functionMetadataManager.AddMetadataAsync(functionMetadata.Key, functionMetadata.Value);
                }
            }
            catch (FunctionMetadataException e)
            {
                Logger.LogError(e, "Exception while deploy new contract.");
                throw;
            }
        }

        public async Task UpdateContract(Address contractAddr, ContractMetadataTemplate oldContractMetadataTemplate,
            ContractMetadataTemplate newContractMetadataTemplate)
        {
            // remove old metadata
            Dictionary<string, FunctionMetadata> tempMap = new Dictionary<string, FunctionMetadata>();
            try
            {
                var globalCallGraph = await GetCallingGraphForChain();
                var newCallGraph =
                    TryRemoveAndGetCallingGraph(contractAddr, globalCallGraph, oldContractMetadataTemplate);
                await _functionMetadataManager.AddCallGraphAsync(SerializeCallingGraph(newCallGraph));
                foreach (var localFuncName in oldContractMetadataTemplate.ProcessFunctionOrder)
                {
                    var funcNameWithAddr = Replacement.ReplaceValueIntoReplacement(localFuncName, Replacement.This,
                        contractAddr.GetFormatted());
                    var funcMetadata = await GetMetadataForNewFunction(funcNameWithAddr,
                        oldContractMetadataTemplate.MethodMetadataTemplates[localFuncName], contractAddr,
                        oldContractMetadataTemplate.ContractReferences, tempMap);

                    tempMap.Add(funcNameWithAddr, funcMetadata);
                }

                foreach (var functionMetadata in tempMap)
                {
                    await _functionMetadataManager.RemoveMetadataAsync(functionMetadata.Key);
                    FunctionMetadataMap.Remove(functionMetadata.Key);
                }

                // deploy newm etadata
                await DeployNewContract(contractAddr, newContractMetadataTemplate);
            }
            catch (FunctionMetadataException e)
            {
                Logger.LogError(e, "Exception while deploy new contract.");
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
        private async Task<FunctionMetadata> GetMetadataForNewFunction(string functionFullName,
            FunctionMetadataTemplate functionTemplate, Address contractAddr,
            Dictionary<string, Address> contractReferences, Dictionary<string, FunctionMetadata> localMetadataMap)
        {
            var resourceSet = new HashSet<Resource>(functionTemplate.LocalResourceSet.Select(resource =>
            {
                var resName =
                    Replacement.ReplaceValueIntoReplacement(resource.Name, Replacement.This,
                        contractAddr.GetFormatted());
                return new Resource(resName, resource.DataAccessMode);
            }));

            var callingSet = new HashSet<string>();

            foreach (var calledFunc in functionTemplate.CallingSet ?? Enumerable.Empty<string>())
            {
                if (!Replacement.TryGetReplacementWithIndex(calledFunc, 0, out var locationReplacement))
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
                        contractAddr.GetFormatted());
                    if (!localMetadataMap.TryGetValue(replacedCalledFunc, out var localCalledFuncMetadata))
                    {
                        throw new FunctionMetadataException("There are no local function " + replacedCalledFunc +
                                                            " in the given local function map, consider wrong reference cause wrong topological order");
                    }

                    resourceSet.UnionWith(localCalledFuncMetadata.FullResourceSet);
                    callingSet.Add(replacedCalledFunc);
                }
                else
                {
                    if (!contractReferences.TryGetValue(Replacement.Value(locationReplacement), out var referenceAddr))
                    {
                        throw new FunctionMetadataException("There are no member reference " +
                                                            Replacement.Value(locationReplacement) +
                                                            " in the given contractReferences map");
                    }

                    var replacedCalledFunc = Replacement.ReplaceValueIntoReplacement(calledFunc, locationReplacement,
                        referenceAddr.GetFormatted());

                    var metadataOfCalledFunc = await GetFunctionMetadata(replacedCalledFunc); //could throw exception

                    resourceSet.UnionWith(metadataOfCalledFunc.FullResourceSet);
                    callingSet.Add(replacedCalledFunc);
                }
            }

            var metadata = new FunctionMetadata(callingSet, resourceSet);

            return metadata;
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="functionFullName"></param>
        /// <returns></returns>
        /// <exception cref="InvalidParameterException"></exception>
        public async Task<FunctionMetadata> GetFunctionMetadata(string functionFullName)
        {
            //BUG: if the smart contract can be updated, then somehow this in-memory cache FunctionMetadataMap need to be updated too. Currently the ChainFunctionMetadata has no way to know some metadata is updated; current thought is to request current "previous block hash" every time the ChainFunctionMetadata public interface got executed, that is "only use cache when in the same block, can clear the cache per block"
            if (!FunctionMetadataMap.TryGetValue(functionFullName, out var txMetadata))
            {
                var data = await _functionMetadataManager.GetMetadataAsync(functionFullName);
                if (data != null)
                {
                    txMetadata = data;
                    FunctionMetadataMap.Add(functionFullName, txMetadata);
                }
                else
                {
                    throw new InvalidParameterException("There are no function named " + functionFullName +
                                                        " in the FunctionMetadataMap");
                }
            }

            return txMetadata;
        }

        public bool UpdataExistingMetadata(string functionFullName, HashSet<string> newOtherFunctionsCallByThis,
            HashSet<string> newNonRecursivePathSet)
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
            callerFunctions = FunctionMetadataMap
                .Where(funcMeta => funcMeta.Value.CallingSet.Contains(calledFunctionFullName))
                .Select(a => a.Key).ToList();

            return callerFunctions.Count != 0;
        }

        #region Calling graph related

        /// <summary>
        /// Try to update the calling graph when updating the function.
        /// If some functions have unknow reference of foreign edge(call other contract's function), the update will not be approved and nothing will take effect
        /// </summary>
        /// <param name=""></param>
        /// <param name="contractAddress"></param>
        /// <param name="callingGraph"></param>
        /// <param name="contractMetadataTemplate"></param>
        /// <returns>The new calling graph</returns>
        /// <exception cref="FunctionMetadataException"></exception>
        public CallGraph TryUpdateAndGetCallingGraph(Address contractAddress, CallGraph callingGraph,
            ContractMetadataTemplate contractMetadataTemplate)
        {
            List<Edge<string>> outEdgesToAdd = new List<Edge<string>>();
            //check for unknown reference
            foreach (var kvPair in contractMetadataTemplate.MethodMetadataTemplates)
            {
                var sourceFunc =
                    Replacement.ReplaceValueIntoReplacement(kvPair.Key, Replacement.This,
                        contractAddress.GetFormatted());

                foreach (var calledFunc in kvPair.Value.CallingSet)
                {
                    if (!calledFunc.Contains(Replacement.This))
                    {
                        Replacement.TryGetReplacementWithIndex(calledFunc, 0, out var memberReplacement);
                        var referenceAddress =
                            contractMetadataTemplate.ContractReferences[
                                Replacement.Value(
                                    memberReplacement)]; //FunctionMetadataTemplate itself ensure this value exist
                        var globalCalledFunc = Replacement.ReplaceValueIntoReplacement(calledFunc, memberReplacement,
                            referenceAddress.GetFormatted());
                        if (!callingGraph.ContainsVertex(globalCalledFunc))
                        {
                            throw new FunctionMetadataException(
                                "Unknow reference of the foreign target in edge <" + sourceFunc + "," + calledFunc +
                                "> when trying to add contract " + contractMetadataTemplate.FullName +
                                " into calling graph, consider the target function does not exist in the metadata");
                        }

                        outEdgesToAdd.Add(new Edge<string>(sourceFunc, globalCalledFunc));
                    }
                }
            }

            //Merge local calling graph, mind that there are functions that call nothing, they also need to appear in the call graph (to be called in future)
            foreach (var localVertex in contractMetadataTemplate.LocalCallingGraph.Vertices)
            {
                var globalVertex =
                    Replacement.ReplaceValueIntoReplacement(localVertex, Replacement.This,
                        contractAddress.GetFormatted());
                callingGraph.AddVertex(globalVertex);
                foreach (var outEdge in contractMetadataTemplate.LocalCallingGraph.OutEdges(localVertex))
                {
                    var toVertex = Replacement.ReplaceValueIntoReplacement(outEdge.Target, Replacement.This,
                        contractAddress.GetFormatted());
                    callingGraph.AddVerticesAndEdge(new Edge<string>(globalVertex, toVertex));
                }
            }

            //add foreign edges
            callingGraph.AddEdgeRange(outEdgesToAdd);

            return callingGraph;
        }

        public CallGraph TryRemoveAndGetCallingGraph(Address contractAddress, CallGraph callingGraph,
            ContractMetadataTemplate contractMetadataTemplate)
        {
            foreach (var kvPair in contractMetadataTemplate.MethodMetadataTemplates)
            {
                var sourceFunc =
                    Replacement.ReplaceValueIntoReplacement(kvPair.Key, Replacement.This,
                        contractAddress.GetFormatted());

                foreach (var calledFunc in kvPair.Value.CallingSet)
                {
                    if (!calledFunc.Contains(Replacement.This))
                    {
                        Replacement.TryGetReplacementWithIndex(calledFunc, 0, out var memberReplacement);
                        var referenceAddress =
                            contractMetadataTemplate.ContractReferences[
                                Replacement.Value(
                                    memberReplacement)]; //FunctionMetadataTemplate itself ensure this value exist
                        var globalCalledFunc = Replacement.ReplaceValueIntoReplacement(calledFunc, memberReplacement,
                            referenceAddress.GetFormatted());
                        if (!callingGraph.ContainsVertex(globalCalledFunc))
                        {
                            throw new FunctionMetadataException(
                                "Unknow reference of the foreign target in edge <" + sourceFunc + "," + calledFunc +
                                "> when trying to add contract " + contractMetadataTemplate.FullName +
                                " into calling graph, consider the target function does not exist in the metadata");
                        }

                        callingGraph.RemoveEdge(new Edge<string>(sourceFunc, globalCalledFunc));
                    }
                }
            }

            foreach (var localVertex in contractMetadataTemplate.LocalCallingGraph.Vertices)
            {
                var globalVertex =
                    Replacement.ReplaceValueIntoReplacement(localVertex, Replacement.This,
                        contractAddress.GetFormatted());
                callingGraph.AddVertex(globalVertex);
                foreach (var outEdge in contractMetadataTemplate.LocalCallingGraph.OutEdges(localVertex))
                {
                    var toVertex = Replacement.ReplaceValueIntoReplacement(outEdge.Target, Replacement.This,
                        contractAddress.GetFormatted());

                    callingGraph.RemoveEdge(new Edge<string>(globalVertex, toVertex));
                    callingGraph.RemoveVertex(globalVertex);
                    callingGraph.RemoveVertex(toVertex);
                }

                callingGraph.RemoveVertex(globalVertex);
            }

            return callingGraph;
        }

        #endregion


        #region Serialize

        private async Task<CallGraph> GetCallingGraphForChain()
        {
            var graphCache = await _functionMetadataManager.GetCallGraphAsync();
            if (graphCache == null)
            {
                return new CallGraph();
            }

            return BuildCallingGraph(graphCache);
        }

        private SerializedCallGraph SerializeCallingGraph(CallGraph graph)
        {
            var serializedCallGraph = new SerializedCallGraph();
            serializedCallGraph.Edges.AddRange(graph.Edges.Select(edge =>
                new GraphEdge {Source = edge.Source, Target = edge.Target}));
            serializedCallGraph.Vertices.AddRange(graph.Vertices);
            return serializedCallGraph;
        }

        /// <summary>
        /// calling graph is prepared for update contract code (check for DAG at that time)
        /// </summary>
        /// <param name="edges"></param>
        /// <returns></returns>
        /// <exception cref="FunctionMetadataException"></exception>
        private CallGraph BuildCallingGraph(SerializedCallGraph callGraph)
        {
            CallGraph graph = new CallGraph();
            graph.AddVertexRange(callGraph.Vertices);
            graph.AddEdgeRange(callGraph.Edges.Select(serializedEdge =>
                new Edge<string>(serializedEdge.Source, serializedEdge.Target)));
            try
            {
                graph.TopologicalSort();
            }
            catch (NonAcyclicGraphException)
            {
                throw new FunctionMetadataException(
                    "The calling graph ISNOT DAG when restoring the calling graph according to the ContractMetadataTemplateMap from the database");
            }

            return graph;
        }

        #endregion
    }
}