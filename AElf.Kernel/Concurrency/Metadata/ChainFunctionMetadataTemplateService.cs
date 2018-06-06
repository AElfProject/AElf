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
        public Dictionary<string, FunctionMetadataTemplate> FunctionMetadataTemplateMap { get; } = new Dictionary<string, FunctionMetadataTemplate>();
        private AdjacencyGraph<string, Edge<string>> _callingGraph;
        
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
            ExtractRawMetadataFromType(contractType, out var smartContractReferenceMap,
                out var localFunctionMetadataTemplateMap);

            TryUpdateCallingGraph(contractType, smartContractReferenceMap, localFunctionMetadataTemplateMap);

            
            //merge the function metadata template map
            localFunctionMetadataTemplateMap.ForEach( kv =>
            {
                var key = Replacement.ReplaceValueIntoReplacement(kv.Key, Replacement.This, contractType.Name);
                FunctionMetadataTemplateMap.Add(key, kv.Value);
            });
            return true;
        }

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
                        throw new FunctionMetadataException("Unknown reference field " + resource +
                                                            " in function " + functionAttribute.FunctionSignature);
                    }
                    return new Resource(resource, dataAccessMode);
                });
                
                if (!localFunctionMetadataTemplateMap.TryAdd(functionAttribute.FunctionSignature, 
                    new FunctionMetadataTemplate(new HashSet<string>(functionAttribute.CallingSet), new HashSet<Resource>(resourceSet))))
                {
                    throw new FunctionMetadataException("Duplicate name of function attribute in contract" + contractType.Name);
                }
            }
        }

        /// <summary>
        /// Try to update the calling graph when updating the function.
        /// If the new graph after applying the update has circle, the update will not be approved and nothing will take effect
        /// </summary>
        /// <param name="contractType"></param>
        /// <param name="smartContractReferenceMap"></param>
        /// <param name="localFunctionMetadataTemplateMap"></param>
        /// <returns></returns>
        /// <exception cref="FunctionMetadataException"></exception>
        private bool TryUpdateCallingGraph(Type contractType, Dictionary<string, Type> smartContractReferenceMap, Dictionary<string, FunctionMetadataTemplate> localFunctionMetadataTemplateMap)
        {
            //check for DAG and update callingGraph if new graph is DAG
            var newGraph = _callingGraph.Clone();

            var replacedCallingSetMap = localFunctionMetadataTemplateMap.Select(kvPair =>
            {
                var functonName = Replacement.ReplaceValueIntoReplacement(kvPair.Key, Replacement.This, contractType.Name);
                var callingSet = kvPair.Value.CallingSet.Select(calledFunc =>
                    Replacement.ReplaceValueIntoReplacement(calledFunc, Replacement.This, contractType.Name));
                return new KeyValuePair<string, IEnumerable<string>>(functonName, callingSet);
            }).ToDictionary(kv => kv.Key, kv=>kv.Value);
            
            foreach (var functionCallingSet in replacedCallingSetMap)
            {
                newGraph.AddVertex(functionCallingSet.Key);
                foreach (var callFunc in functionCallingSet.Value)
                {
                    if (callFunc.Contains(contractType.Name))
                    {
                        if (replacedCallingSetMap.ContainsKey(callFunc))
                        {
                            newGraph.AddVerticesAndEdge(new Edge<string>(functionCallingSet.Key, callFunc));
                        }
                        else
                        {
                            throw new FunctionMetadataException(
                                "calling set of function " + functionCallingSet.Key + " when adding contract " +
                                contractType.Name + " contains unknown reference to it's own function: " +
                                callFunc);
                        }
                    }
                    else
                    {
                        string callFuncWithClassName;
                        if (Replacement.TryGetReplacementWithIndex(callFunc, 0, out var memberReplacement) && smartContractReferenceMap.ContainsKey(Replacement.Value(memberReplacement)))
                        {
                            Type referenceType = smartContractReferenceMap[Replacement.Value(memberReplacement)];
                            callFuncWithClassName = Replacement.ReplaceValueIntoReplacement(callFunc, memberReplacement,
                                referenceType.Name);
                        }
                        else
                        {
                            throw new FunctionMetadataException(
                                "calling set of function " + functionCallingSet.Key + " when adding contract " +
                                contractType.Name + " contains unknown reference to other contract's function: " +
                                callFunc);
                        }
                        
                        if (newGraph.ContainsVertex(callFuncWithClassName))
                        {
                            newGraph.AddEdge(new Edge<string>(functionCallingSet.Key, callFuncWithClassName));
                        }
                        else
                        {
                            throw new FunctionMetadataException(
                                "calling set of function " + functionCallingSet.Key + " when adding contract " +
                                contractType.Name + " contains unknown reference to other contract's function: " +
                                callFunc);
                        }
                    }
                }
            }
            
            try
            {
                newGraph.TopologicalSort();
            }
            catch (NonAcyclicGraphException e)
            {
                _logger?.Warn(e, "When Add template for " + contractType.Name + ", its calling set form a acyclic graph");
                return false;
            }

            _callingGraph = newGraph;
            return true;
        }

        private Dictionary<string, FunctionMetadataTemplate> CompleteLocalResource(Dictionary<string, FunctionMetadataTemplate> localFunctionMetadataTemplateMap)
        {
            var completeTemplateMap = new Dictionary<string, FunctionMetadataTemplate>();
            foreach (var kvPair in localFunctionMetadataTemplateMap)
            {
                if (!completeTemplateMap.ContainsKey(kvPair.Key))
                {
                    GetAndAddCompleteLocalResourceSetForFunction(kvPair.Key, localFunctionMetadataTemplateMap, ref completeTemplateMap);
                }
            }

            return completeTemplateMap;
        }

        private FunctionMetadataTemplate GetAndAddCompleteLocalResourceSetForFunction(string localFuncName,
            Dictionary<string, FunctionMetadataTemplate> nonCompleteMetadataTemplateMap, ref Dictionary<string, FunctionMetadataTemplate> completeTemplateMap)
        {
            if (!completeTemplateMap.ContainsKey(localFuncName))
            {
                HashSet<Resource> resourceSet = new HashSet<Resource>(nonCompleteMetadataTemplateMap[localFuncName].LocalResourceSet);
                foreach (var calledFunc in nonCompleteMetadataTemplateMap[localFuncName].CallingSet)
                {
                    if (calledFunc.Contains(Replacement.This))
                    {
                        var completeCallingTemplate = GetAndAddCompleteLocalResourceSetForFunction(calledFunc, nonCompleteMetadataTemplateMap,
                            ref completeTemplateMap);
                        resourceSet.UnionWith(completeCallingTemplate.LocalResourceSet);
                    }
                }
                
                var completeTemplate = new FunctionMetadataTemplate(nonCompleteMetadataTemplateMap[localFuncName].CallingSet, resourceSet);
                completeTemplateMap.Add(localFuncName, completeTemplate);
                return completeTemplate;
            }
            else
            {
                return completeTemplateMap[localFuncName];
            }
        }
        
        #endregion
    }

    internal class FunctionMetadataException : Exception
    {
        internal FunctionMetadataException(string msg) : base(msg)
        {
            
        }
    }

    internal class Replacement
    {
        public static string ReplacementRegexPattern = @"\$\{[a-zA-Z_][a-zA-Z0-9_]*((\.[a-zA-Z_][a-zA-Z0-9_]*)*)\}";
        
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
            var replacements = Regex.Matches(str, Replacement.ReplacementRegexPattern);
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