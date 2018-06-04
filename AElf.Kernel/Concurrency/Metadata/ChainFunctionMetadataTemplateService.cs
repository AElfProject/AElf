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
using ServiceStack;
using FunctionMetadataTemplate = AElf.Kernel.Concurrency.Metadata.FunctionMetadata;

namespace AElf.Kernel.Concurrency.Metadata
{
    /// <summary>
    /// Where get and set the metadata when deploy the contracts and check correctness when trying to updating contracts(functions)
    /// TODO: currently just sopport update one function of a contract, if trying to update multiple function at a time, the calling graph and the FunctionMetadataMap should be backup before the update in case one of the function fail the update and all the preceding updated function need to roll back their effect. Or maybe just check Whether applying all the updating functions can result in non-DAG calling graph. 
    /// TODO: Whether need the functionality of deleting the existing function?
    /// </summary>
    public class ChainFunctionMetadataTemplateService : IChainFunctionMetadataTemplateService
    {
        public Dictionary<string, FunctionMetadataTemplate> FunctionMetadataTemplateMap { get; } = new Dictionary<string, FunctionMetadataTemplate>();
        public readonly Dictionary<string, DataAccessMode> ResourceAccessModes;
        private AdjacencyGraph<string, Edge<string>> _callingGraph;
        
        private readonly ILogger _logger;

        public ChainFunctionMetadataTemplateService(ILogger logger = null)
        {
            _logger = logger;
            _callingGraph = new AdjacencyGraph<string, Edge<string>>();
            ResourceAccessModes = new Dictionary<string, DataAccessMode>();
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
            ExtractRawMetadataFromType(contractType, out var localFieldMap, out var smartContractReferenceMap,
                out var localFunctionMetadataTemplateMap);

            TryUpdateCallingGraph(contractType, smartContractReferenceMap, localFunctionMetadataTemplateMap);
            
            //merge the function metadata template map
            foreach (var localMetadata in localFunctionMetadataTemplateMap)
            {
                FunctionMetadataTemplateMap.Add(localMetadata.Key, localMetadata.Value);
            }

            foreach (var field in localFieldMap)
            {
                ResourceAccessModes.TryAdd(field.Key, field.Value);
            }
            return true;
        }

        private void ExtractRawMetadataFromType(Type contractType, out Dictionary<string, DataAccessMode> localFieldMap, out Dictionary<string, Type> smartContractReferenceMap, out Dictionary<string, FunctionMetadataTemplate> localFunctionMetadataTemplateMap )
        {
            localFieldMap = new Dictionary<string, DataAccessMode>();
            smartContractReferenceMap = new Dictionary<string, Type>();
            localFunctionMetadataTemplateMap = new Dictionary<string, FunctionMetadataTemplate>();

            
            
            foreach (var fieldInfo in contractType.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
            {
                var fieldAttr = fieldInfo.GetCustomAttribute<SmartContractFieldDataAttribute>();
                if (fieldAttr == null) continue;
                if (!localFieldMap.TryAdd(ReplaceValueIntoReplacement(fieldAttr.FieldName, Replacement.This, Replacement.ContractType(contractType)), fieldAttr.DataAccessMode))
                {
                    throw new FunctionMetadataException("Duplicate name of field attributes in contract " + contractType.Name);
                }
            }
            
            foreach (var fieldInfo in contractType.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
            {
                var smartContractRefAttr = fieldInfo.GetCustomAttribute<SmartContractReferenceAttribute>();
                if (smartContractRefAttr == null) continue;
                if (!smartContractReferenceMap.TryAdd(smartContractRefAttr.FieldName, smartContractRefAttr.ContractType))
                {
                    throw new FunctionMetadataException("Duplicate name of smart contract reference attributes in contract " + contractType.Name);
                }
            }
            
            foreach (var methodInfo in contractType.GetMethods(
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
            {
                var functionAttribute = methodInfo.GetCustomAttribute<SmartContractFunctionAttribute>();
                if (functionAttribute == null) continue;


                var replacedCallingSet = functionAttribute.CallingSet.Select(func =>
                    ReplaceValueIntoReplacement(func, Replacement.This, Replacement.ContractType(contractType)));

                var replacedResourceSet = functionAttribute.LocalResources.Select(resource =>
                    ReplaceValueIntoReplacement(resource, Replacement.This, Replacement.ContractType(contractType)));

                var replacedFunctionName = ReplaceValueIntoReplacement(functionAttribute.FunctionSignature,
                    Replacement.This, Replacement.ContractType(contractType));
                
                if (!localFunctionMetadataTemplateMap.TryAdd(replacedFunctionName, 
                    new FunctionMetadataTemplate(new HashSet<string>(replacedCallingSet), new HashSet<string>(replacedResourceSet))))
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
            foreach (var functionMetadata in localFunctionMetadataTemplateMap)
            {
                newGraph.AddVertex(functionMetadata.Key);
                foreach (var callFunc in functionMetadata.Value.CallingSet)
                {
                    if (callFunc.Contains(Replacement.ContractType(contractType)))
                    {
                        if (localFunctionMetadataTemplateMap.ContainsKey(callFunc))
                        {
                            newGraph.AddVerticesAndEdge(new Edge<string>(functionMetadata.Key, callFunc));
                        }
                        else
                        {
                            throw new FunctionMetadataException(
                                "calling set of function " + functionMetadata.Key + " when adding contract " +
                                contractType.Name + " contains unknown reference to it's own function: " +
                                callFunc);
                        }
                    }
                    else
                    {
                        string callFuncWithClassName;
                        if (TryGetReplacementWithIndex(callFunc, 0, out var memberReplacement) && smartContractReferenceMap.ContainsKey(Replacement.Value(memberReplacement)))
                        {
                            Type referenceType = smartContractReferenceMap[Replacement.Value(memberReplacement)];
                            callFuncWithClassName = ReplaceValueIntoReplacement(callFunc, memberReplacement,
                                Replacement.ContractType(referenceType));
                        }
                        else
                        {
                            throw new FunctionMetadataException(
                                "calling set of function " + functionMetadata.Key + " when adding contract " +
                                contractType.Name + " contains unknown reference to other contract's function: " +
                                callFunc);
                        }
                        
                        if (newGraph.ContainsVertex(callFuncWithClassName))
                        {
                            newGraph.AddEdge(new Edge<string>(functionMetadata.Key, callFuncWithClassName));
                        }
                        else
                        {
                            throw new FunctionMetadataException(
                                "calling set of function " + functionMetadata.Key + " when adding contract " +
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
        
        private string ReplaceValueIntoReplacement(string str, string replacement, string value)
        {
            return str.Replace(replacement, value);
        }

        private bool TryGetReplacementWithIndex(string str, int index, out string res)
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
    }
}