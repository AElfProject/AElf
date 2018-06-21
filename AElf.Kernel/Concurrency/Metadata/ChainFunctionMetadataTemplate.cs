using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using AElf.Kernel.Extensions;
using AElf.Kernel.Storages;
using Akka.Util.Internal;
using Google.Protobuf;
using NLog;
using Org.BouncyCastle.Security;
using QuickGraph;
using QuickGraph.Algorithms;

namespace AElf.Kernel.Concurrency.Metadata
{
    /// <summary>
    /// Where get and set the metadata when deploy the contracts and check correctness when trying to updating contracts(functions)
    /// TODO: Whether need the functionality of deleting the existing function?
    /// </summary>
    public class ChainFunctionMetadataTemplate : IChainFunctionMetadataTemplate
    {
        public Dictionary<string, Dictionary<string, FunctionMetadataTemplate>> ContractMetadataTemplateMap { get; private set; } 
        public AdjacencyGraph<string, Edge<string>> CallingGraph; //calling graph is prepared for update contract code (check for DAG at that time)
        
        private readonly ILogger _logger;
        private readonly IDataStore _dataStore;
        public Hash ChainId { get;}
        

        public ChainFunctionMetadataTemplate(IDataStore dataStore, Hash chainId, ILogger logger = null)
        {
            _dataStore = dataStore;
            _logger = logger;
            ChainId = chainId;
            
            var mapCache = _dataStore.GetDataAsync(Path.CalculatePointerForMetadataTemlate(chainId)).Result;
            var graphCache = _dataStore.GetDataAsync(Path.CalculatePointerForMetadataTemlateCallingGraph(chainId))
                .Result;
            if (mapCache != null)
            {
                ContractMetadataTemplateMap =
                    ReadFromSerializeContractMetadataTemplateMap(
                        SerializeContractMetadataTemplateMap.Parser.ParseFrom(mapCache));
                if (graphCache == null)
                {
                    throw new FunctionMetadataException("ChainId [" + ChainId.Value + "] Cannot find calling graph in database");
                }
                CallingGraph = RestoreCallingGraph(CallingGraphEdges.Parser.ParseFrom(graphCache));
            }
            else
            {
                ContractMetadataTemplateMap = new Dictionary<string, Dictionary<string, FunctionMetadataTemplate>>();
                CallingGraph = new AdjacencyGraph<string, Edge<string>>();
            }
        }

        #region Metadata extraction from contract code

        /// <summary>
        /// 1. extract attributes in type
        /// 2. check whether this new calling graph is DAG
        /// 3. Return new class's function metadata into map
        /// </summary>
        /// <param name="contractType"></param>
        /// <returns></returns>
        /// <exception cref="FunctionMetadataException"></exception>
        public async Task<bool> TryAddNewContract(Type contractType)
        {
            Ready();
            try
            {
                ExtractRawMetadataFromType(contractType, out var smartContractReferenceMap,
                    out var localFunctionMetadataTemplateMap);
                
                UpdateTemplate(contractType, smartContractReferenceMap, ref localFunctionMetadataTemplateMap);
                
                //merge the function metadata template map
                ContractMetadataTemplateMap.Add(contractType.Name, localFunctionMetadataTemplateMap);
            }
            catch (FunctionMetadataException e)
            {
                _logger?.Error(e, e.Message);
                throw;
            }

            //TODO: now each call of this will have large Disk IO because we replace the new whole map into the old map even if just minor changes to the map
            await _dataStore.SetDataAsync(Path.CalculatePointerForMetadataTemlate(ChainId),
                GetSerializeContractMetadataTemplateMap().ToByteArray());
            await _dataStore.SetDataAsync(Path.CalculatePointerForMetadataTemlateCallingGraph(ChainId),
                GetSerializeCallingGraph().ToByteArray());
            
            return true;
        }

        
        

        /// <summary>
        /// FunctionMetadataException will be thrown in following cases: 
        /// (1) Duplicate member function name.
        /// (2) Local resource are not declared in the code.
        /// (3) Duplicate smart contract reference name
        /// (4) Duplicate declared field name.
        /// (5) Unknown reference in calling set
        /// </summary>
        /// <param name="contractType"></param>
        /// <param name="smartContractReferenceMap"></param>
        /// <param name="localFunctionMetadataTemplateMap"></param>
        /// <exception cref="FunctionMetadataException"></exception>
        private void ExtractRawMetadataFromType(Type contractType, out Dictionary<string, Type> smartContractReferenceMap, out Dictionary<string, FunctionMetadataTemplate> localFunctionMetadataTemplateMap )
        {
            var templocalFieldMap = new Dictionary<string, DataAccessMode>();
            smartContractReferenceMap = new Dictionary<string, Type>();
            localFunctionMetadataTemplateMap = new Dictionary<string, FunctionMetadataTemplate>();

            //load localFieldMap: <"${this}.[ResourceName]", DataAccessMode>
            foreach (var fieldInfo in contractType.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
            {
                var fieldAttr = fieldInfo.GetCustomAttribute<SmartContractFieldDataAttribute>();
                if (fieldAttr == null) continue;
                if (!templocalFieldMap.TryAdd(fieldAttr.FieldName, fieldAttr.DataAccessMode))
                {
                    throw new FunctionMetadataException("ChainId [" + ChainId.Value + "] Duplicate name of field attributes in contract " + contractType.Name);
                }
            }
            
            //load smartContractReferenceMap: <"[contract_member_name]", Referenced contract type>
            foreach (var fieldInfo in contractType.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
            {
                var smartContractRefAttr = fieldInfo.GetCustomAttribute<SmartContractReferenceAttribute>();
                if (smartContractRefAttr == null) continue;
                if (!smartContractReferenceMap.TryAdd(smartContractRefAttr.FieldName, smartContractRefAttr.ContractType))
                {
                    throw new FunctionMetadataException("ChainId [" + ChainId.Value + "] Duplicate name of smart contract reference attributes in contract " + contractType.Name);
                }
            }
            
            //load localFunctionMetadataTemplateMap: <"${[this]}.FunctionSignature", FunctionMetadataTemplate>
            //FunctionMetadataTemplate: <calling_set, local_resource_set>
            //calling_set: { "${[contract_member_name]}.[FunctionSignature]", ${this}.[FunctionSignature]... }
            //local_resource_set: {"${this}.[ResourceName]"}
            foreach (var methodInfo in contractType.GetMethods(
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
            {
                var functionAttribute = methodInfo.GetCustomAttribute<SmartContractFunctionAttribute>();
                if (functionAttribute == null) continue;

                var resourceSet = functionAttribute.LocalResources.Select(resource =>
                {
                    if (!templocalFieldMap.TryGetValue(resource, out var dataAccessMode))
                    {
                        throw new FunctionMetadataException("ChainId [" + ChainId.Value + "] Unknown reference local field " + resource +
                                                            " in function " + functionAttribute.FunctionSignature);
                    }
                    return new Resource(resource, dataAccessMode);
                });
                
                if (!localFunctionMetadataTemplateMap.TryAdd(functionAttribute.FunctionSignature, 
                    new FunctionMetadataTemplate(new HashSet<string>(functionAttribute.CallingSet), new HashSet<Resource>(resourceSet))))
                {
                    throw new FunctionMetadataException("ChainId [" + ChainId.Value + "] Duplicate name of function attribute" + functionAttribute.FunctionSignature + " in contract" + contractType.Name);
                }
            }

            if (localFunctionMetadataTemplateMap.Count == 0)
            {
                throw new FunctionMetadataException("ChainId [" + ChainId.Value + " no function marked in the target contract " + contractType.Name);
            }
            
            //check for validaty of the calling set (whether have unknow reference)
            foreach (var kvPair in localFunctionMetadataTemplateMap)
            {
                foreach (var calledFunc in kvPair.Value.CallingSet)
                {
                    if (calledFunc.Contains(Replacement.This))
                    {
                        if (!localFunctionMetadataTemplateMap.ContainsKey(calledFunc))
                        {
                            throw new FunctionMetadataException(
                                "ChainId [" + ChainId.Value + "] calling set of function " + kvPair.Key + " when adding contract " +
                                contractType.Name + " contains unknown reference to it's own function: " +
                                calledFunc);
                        }
                    }
                    else
                    {
                        if (!Replacement.TryGetReplacementWithIndex(calledFunc, 0, out var memberReplacement) ||
                            !smartContractReferenceMap.ContainsKey(Replacement.Value(memberReplacement)))
                        {
                            throw new FunctionMetadataException(
                                "ChainId [" + ChainId.Value + "] calling set of function " + kvPair.Key + " when adding contract " +
                                contractType.Name + " contains unknown local member reference to other contract: " +
                                calledFunc);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Try to update the calling graph when updating the function.
        /// If the new graph after applying the update has circle (or have unknow reference of foreign edge(call other contract's function)), the update will not be approved and nothing will take effect
        /// </summary>
        /// <param name="contractType"></param>
        /// <param name="smartContractReferenceMap"></param>
        /// <param name="targetLocalFunctionMetadataTemplateMap"></param>
        /// <returns></returns>
        /// <exception cref="FunctionMetadataException"></exception>
        private void UpdateTemplate(Type contractType, Dictionary<string, Type> smartContractReferenceMap, ref Dictionary<string, FunctionMetadataTemplate> targetLocalFunctionMetadataTemplateMap)
        {
            //check for DAG  (the updating calling graph is DAG iff local calling graph is DAG)
            if (!TryGetLocalCallingGraph(targetLocalFunctionMetadataTemplateMap, out var localCallGraph, out var localTopologicRes))
            {
                throw new FunctionMetadataException("ChainId [" + ChainId.Value + "] Calling graph of " + contractType.Name + " is Non-DAG thus nothing take effect");
            }

            
            List<Edge<string>> outEdgesToAdd = new List<Edge<string>>();
            
            //check for unknown reference
            foreach (var kvPair in targetLocalFunctionMetadataTemplateMap)
            {
                var sourceFunc = Replacement.ReplaceValueIntoReplacement(kvPair.Key, Replacement.This, contractType.Name);
                foreach (var calledFunc in kvPair.Value.CallingSet)
                {
                    if (!calledFunc.Contains(Replacement.This))
                    {
                        Replacement.TryGetReplacementWithIndex(calledFunc, 0, out var memberReplacement);
                        Type referenceType = smartContractReferenceMap[Replacement.Value(memberReplacement)];
                        var globalCalledFunc = Replacement.ReplaceValueIntoReplacement(calledFunc, memberReplacement,
                            referenceType.Name);
                        if (!CallingGraph.ContainsVertex(globalCalledFunc))
                        {
                            throw new FunctionMetadataException("ChainId [" + ChainId.Value + "] Unknow reference of the foreign target in edge <" + sourceFunc + ","+calledFunc+"> when trying to add contract " + contractType.Name + " into calling graph, consider the target function does not exist in the foreign contract");
                        }
                        outEdgesToAdd.Add(new Edge<string>(sourceFunc, globalCalledFunc));
                    }
                }
            }
            
            //Merge local calling graph
            foreach (var localVertex in localCallGraph.Vertices)
            {
                var globalVertex = Replacement.ReplaceValueIntoReplacement(localVertex, Replacement.This, contractType.Name);
                CallingGraph.AddVertex(globalVertex);
                foreach (var outEdge in localCallGraph.OutEdges(localVertex))
                {
                    var toVertex = Replacement.ReplaceValueIntoReplacement(outEdge.Target, Replacement.This, contractType.Name);
                    CallingGraph.AddVerticesAndEdge(new Edge<string>(globalVertex, toVertex));
                }
            }
            //add foreign edges
            CallingGraph.AddEdgeRange(outEdgesToAdd);
        }
        
        
        public bool TryGetLocalCallingGraph(Dictionary<string, FunctionMetadataTemplate> localFunctionMetadataTemplateMap, out AdjacencyGraph<string, Edge<string>> callGraph, out IEnumerable<string> topologicRes)
        {
            Ready();
            callGraph = new AdjacencyGraph<string, Edge<string>>();
            foreach (var kvPair in localFunctionMetadataTemplateMap)
            {
                callGraph.AddVertex(kvPair.Key);
                foreach (var calledFunc in kvPair.Value.CallingSet)
                { 
                    if (calledFunc.Contains(Replacement.This))
                    {
                        callGraph.AddVerticesAndEdge(new Edge<string>(kvPair.Key, calledFunc));
                    }
                }
            }

            try
            {
                topologicRes = callGraph.TopologicalSort();
            }
            catch (NonAcyclicGraphException)
            {
                callGraph.Clear();
                callGraph = null;
                topologicRes = null;
                return false;
            }
            
            return true;
        }
        #endregion

        #region Serialization

        private SerializeContractMetadataTemplateMap GetSerializeContractMetadataTemplateMap()
        {
            var serializeMap = new SerializeContractMetadataTemplateMap();
            foreach (var kv in ContractMetadataTemplateMap)
            {
                var functionMetadataMapForContract = new SerializeFunctionMetadataTemplateMap();
                foreach (var funcTempalteMap in kv.Value)
                {
                    functionMetadataMapForContract.TemplateMap.Add(funcTempalteMap.Key, funcTempalteMap.Value);
                }
                serializeMap.MetadataTemplateMapForContract.Add(kv.Key, functionMetadataMapForContract);
            }

            return serializeMap;
        }
        
        private dynamic ReadFromSerializeContractMetadataTemplateMap(SerializeContractMetadataTemplateMap serializeMap)
        {
            var contractMetadataMap = new Dictionary<string, Dictionary<string, FunctionMetadataTemplate>>();
            foreach (var kv in serializeMap.MetadataTemplateMapForContract)
            {
                var functionMetadataMapForContract = new Dictionary<string, FunctionMetadataTemplate>();
                foreach (var funcTempalteMap in kv.Value.TemplateMap)
                {
                    functionMetadataMapForContract.Add(funcTempalteMap.Key, funcTempalteMap.Value);
                }
                contractMetadataMap.Add(kv.Key, functionMetadataMapForContract);
            }

            return contractMetadataMap;
        }

        private CallingGraphEdges GetSerializeCallingGraph()
        {
            var serializeCallingGraph = new CallingGraphEdges();
            serializeCallingGraph.Edges.AddRange(CallingGraph.Edges.Select(edge =>
                new GraphEdge {Source = edge.Source, Target = edge.Target}));
            return serializeCallingGraph;
        }

        private dynamic RestoreCallingGraph(CallingGraphEdges edges)
        {
            AdjacencyGraph<string, Edge<string>> graph = new AdjacencyGraph<string, Edge<string>>();
            graph.AddVerticesAndEdgeRange(edges.Edges.Select(kv => new Edge<string>(kv.Source, kv.Target)));
            try
            {
                graph.TopologicalSort();
            }
            catch (NonAcyclicGraphException)
            {
                throw new FunctionMetadataException("ChainId [" + ChainId.Value + "] The calling graph ISNOT DAG when restoring the calling graph according to the ContractMetadataTemplateMap from the database");
            }
            return graph;
        }

        private void Ready()
        {
            if (ChainId == null)
            {
                throw new FunctionMetadataException("ChainId not set for tempalte service ");
            }
        }

        #endregion
    }

    public class FunctionMetadataException : Exception
    {
        internal FunctionMetadataException(string msg) : base(msg)
        {
            
        }
    }

    internal static class Replacement
    {
        private static string ReplacementRegexPattern = @"\$\{[a-zA-Z_][a-zA-Z0-9_]*((\.[a-zA-Z_][a-zA-Z0-9_]*)*)\}";
        
        public static readonly string This = "${this}";

        public static string ContractType(Type contractType)
        {
            return "${" + contractType.Name + "}";
        }

        public static string Value(string replacement)
        {
            if (Regex.Match(replacement, ReplacementRegexPattern).Value.Equals(replacement))
            {
                return replacement.Substring(2, replacement.Length - 3);
            }
            else
            {
                throw new InvalidParameterException("The input value: " + replacement + "is not a replacement");
            }
        }
        
        public static string ReplaceValueIntoReplacement(string str, string replacement, string value)
        {
            return str.Replace(replacement, value);
        }
        
        public static bool TryGetReplacementWithIndex(string str, int index, out string res)
        {
            var replacements = Regex.Matches(str, ReplacementRegexPattern);
            if (index < replacements.Count)
            {
                res = replacements[index].Value;
                return true;
            }
            else
            {
                res = null;
                return false;
            }
        }
    }
}