using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using Akka.Util.Internal;
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
    public class ChainFunctionMetadataTemplateService : IChainFunctionMetadataTemplateService
    {
        public Dictionary<string, Dictionary<string, FunctionMetadataTemplate>> ContractMetadataTemplateMap { get; } = new Dictionary<string, Dictionary<string, FunctionMetadataTemplate>>();
        private readonly AdjacencyGraph<string, Edge<string>> _callingGraph; //calling graph is prepared for update contract code (check for DAG at that time)
        
        private readonly ILogger _logger;

        public ChainFunctionMetadataTemplateService(ILogger logger = null)
        {
            _logger = logger;
            _callingGraph = new AdjacencyGraph<string, Edge<string>>();
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
        public bool TryAddNewContract(Type contractType)
        {
            try
            {
                ExtractRawMetadataFromType(contractType, out var smartContractReferenceMap,
                    out var localFunctionMetadataTemplateMap);
                
                CompleteLocalResourceAndUpdateTemplate(contractType, smartContractReferenceMap, ref localFunctionMetadataTemplateMap);
                
                //merge the function metadata template map
                ContractMetadataTemplateMap.Add(contractType.Name, localFunctionMetadataTemplateMap);
            }
            catch (FunctionMetadataException e)
            {
                _logger?.Error(e, e.Message);
                throw;
            }
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
                    throw new FunctionMetadataException("Duplicate name of field attributes in contract " + contractType.Name);
                }
            }
            
            //load smartContractReferenceMap: <"[contract_member_name]", Referenced contract type>
            foreach (var fieldInfo in contractType.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
            {
                var smartContractRefAttr = fieldInfo.GetCustomAttribute<SmartContractReferenceAttribute>();
                if (smartContractRefAttr == null) continue;
                if (!smartContractReferenceMap.TryAdd(smartContractRefAttr.FieldName, smartContractRefAttr.ContractType))
                {
                    throw new FunctionMetadataException("Duplicate name of smart contract reference attributes in contract " + contractType.Name);
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
                        throw new FunctionMetadataException("Unknown reference local field " + resource +
                                                            " in function " + functionAttribute.FunctionSignature);
                    }
                    return new Resource(resource, dataAccessMode);
                });
                
                if (!localFunctionMetadataTemplateMap.TryAdd(functionAttribute.FunctionSignature, 
                    new FunctionMetadataTemplate(new HashSet<string>(functionAttribute.CallingSet), new HashSet<Resource>(resourceSet))))
                {
                    throw new FunctionMetadataException("Duplicate name of function attribute" + functionAttribute.FunctionSignature + " in contract" + contractType.Name);
                }
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
                                "calling set of function " + kvPair.Key + " when adding contract " +
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
                                "calling set of function " + kvPair.Key + " when adding contract " +
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
        private void CompleteLocalResourceAndUpdateTemplate(Type contractType, Dictionary<string, Type> smartContractReferenceMap, ref Dictionary<string, FunctionMetadataTemplate> targetLocalFunctionMetadataTemplateMap)
        {
            //check for DAG  (the updating calling graph is DAG iff local calling graph is DAG)
            if (!TryGetLocalCallingGraph(targetLocalFunctionMetadataTemplateMap, out var localCallGraph, out var localTopologicRes))
            {
                throw new FunctionMetadataException("Calling graph of " + contractType.Name + " is Non-DAG thus nothing take effect");
            }

            CompleteLocalResourceInTemplates(ref targetLocalFunctionMetadataTemplateMap, localTopologicRes);
            
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
                        if (!_callingGraph.ContainsVertex(globalCalledFunc))
                        {
                            throw new FunctionMetadataException("Unknow reference of the foreign target in edge <" + sourceFunc + ","+calledFunc+"> when trying to add contract " + contractType.Name + " into calling graph, consider the target function does not exist in the foreign contract");
                        }
                        outEdgesToAdd.Add(new Edge<string>(sourceFunc, globalCalledFunc));
                    }
                }
            }
            
            //Merge local calling graph
            foreach (var localVertex in localCallGraph.Vertices)
            {
                var globalVertex = Replacement.ReplaceValueIntoReplacement(localVertex, Replacement.This, contractType.Name);
                _callingGraph.AddVertex(globalVertex);
                foreach (var outEdge in localCallGraph.OutEdges(localVertex))
                {
                    var toVertex = Replacement.ReplaceValueIntoReplacement(outEdge.Target, Replacement.This, contractType.Name);
                    _callingGraph.AddVerticesAndEdge(new Edge<string>(globalVertex, toVertex));
                }
            }
            //add foreign edges
            _callingGraph.AddEdgeRange(outEdgesToAdd);
        }
        
        
        public bool TryGetLocalCallingGraph(Dictionary<string, FunctionMetadataTemplate> localFunctionMetadataTemplateMap, out AdjacencyGraph<string, Edge<string>> callGraph, out IEnumerable<string> topologicRes)
        {
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

        /// <summary>
        /// Use topologicalOrder (reverse it inside this function) to calculate compelete local resource without dependency issue.
        /// Should determine whether calling graph is DAG or not, this function take the topologicalOrder to complete local resource without checking
        /// 
        /// Note: function ExtractRawMetadataFromType has confirmed that no unknow reference in calling set so just access it without checking
        /// </summary>
        /// <param name="nonCompleteMetadataTemplateMap"></param>
        /// <param name="topologicOrder"></param>
        /// <returns>local template with complete resource. (by local, we mean replacement is still ${this} or else local replacement)</returns>
        private void CompleteLocalResourceInTemplates(ref Dictionary<string, FunctionMetadataTemplate> nonCompleteMetadataTemplateMap, IEnumerable<string> topologicOrder)
        {
            
            var completeTemplateMap = new Dictionary<string, FunctionMetadataTemplate>();
            foreach (var funcName in topologicOrder.Reverse())
            {
                HashSet<Resource> resourceSet = new HashSet<Resource>(nonCompleteMetadataTemplateMap[funcName].LocalResourceSet);
                var callingSet = nonCompleteMetadataTemplateMap[funcName].CallingSet;
                foreach (var calledFunc in callingSet)
                {
                    if (calledFunc.Contains(Replacement.This))
                    {
                        if (completeTemplateMap.TryGetValue(calledFunc, out var functionMetadataTemplate))
                        {
                            resourceSet.UnionWith(functionMetadataTemplate.LocalResourceSet);
                        }
                    }
                }
                completeTemplateMap.Add(funcName, new FunctionMetadataTemplate(callingSet, resourceSet));
            }

            nonCompleteMetadataTemplateMap = completeTemplateMap;
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