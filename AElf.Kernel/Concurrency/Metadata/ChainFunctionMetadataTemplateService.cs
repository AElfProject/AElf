using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using Akka.Util.Internal;
using NLog;
using Org.BouncyCastle.Crypto.Prng;
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
        public Dictionary<string, DataAccessMode> ResourceAccessModes;
        public Dictionary<string, Type> ReferenceTypes;
        private AdjacencyGraph<string, Edge<string>> _callingGraph;
        
        private readonly ILogger _logger;

        public ChainFunctionMetadataTemplateService(ILogger logger = null)
        {
            _logger = logger;
            _callingGraph = new AdjacencyGraph<string, Edge<string>>();
        }

        public bool TryAddNewContract(Type contractCode)
        {
            throw new NotImplementedException();
        }

        public bool SetNewFunctionMetadata(string functionFullName, HashSet<string> otherFunctionsCallByThis, HashSet<string> nonRecursivePathSet)
        {
            if (FunctionMetadataTemplateMap.ContainsKey(functionFullName))
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
            
            FunctionMetadataTemplateMap.Add(functionFullName, metadata);

            //add the new function into calling graph
            //this graph will still be DAG cause GetFunctionMetadata above will throw exception if it's not
            _callingGraph.AddVertex(functionFullName);
            foreach (var callingFunc in metadata.CallingSet)
            {
                _callingGraph.AddEdge(new Edge<string>(functionFullName, callingFunc));
            }
            return true;
        }

        public FunctionMetadata GetFunctionMetadata(string functionFullName)
        {
            if (FunctionMetadataTemplateMap.TryGetValue(functionFullName, out var txMetadata))
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
            if (!FunctionMetadataTemplateMap.ContainsKey(functionFullName))
            {
                throw new InvalidOperationException("FunctionMetadataMap don't contain a function named " + functionFullName + " when trying to update this function's metadata");
            }
            
            var oldMetadata = FunctionMetadataTemplateMap[functionFullName];

            if (!TryUpdateCallingGraph(functionFullName, oldMetadata.CallingSet, newOtherFunctionsCallByThis))
            {
                //new graph have circle, nothing take effect
                return false;
            }

            FunctionMetadataTemplateMap.Remove(functionFullName);
            
            if(!SetNewFunctionMetadata(functionFullName, newOtherFunctionsCallByThis, newNonRecursivePathSet)){
                //This should be unReachable, because function above already check whether new graph is DAG
                FunctionMetadataTemplateMap.Add(functionFullName, oldMetadata);
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
                    var oldMetadata = FunctionMetadataTemplateMap[caller];
                    FunctionMetadataTemplateMap.Remove(caller);
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
            callerFunctions = FunctionMetadataTemplateMap.Where(funcMeta => funcMeta.Value.CallingSet.Contains(calledFunctionFullName))
                .Select(a => a.Key).ToList();
            
            return !callerFunctions.IsEmpty();
        }

        /// <summary>
        /// Try to update the calling graph when updating the function.
        /// If the new graph after applying the update has circle, the update will not be approved and nothing will take effect
        /// </summary>
        /// <param name="updatingFunc"></param>
        /// <param name="oldCallingSet"></param>
        /// <param name="newOtherFunctionsCallByThis"></param>
        /// <returns></returns>
        private bool TryUpdateCallingGraph(string updatingFunc, HashSet<string> oldCallingSet, HashSet<string> newOtherFunctionsCallByThis)
        {
            var newGraph = _callingGraph.CreateCopy();
            newGraph.RemoveOutEdgeIf(updatingFunc, outEdge => oldCallingSet.Contains(outEdge.Target));
            
            foreach (var newCallingFunc in newOtherFunctionsCallByThis)
            {
                newGraph.AddEdge(new Edge<string>(updatingFunc, newCallingFunc));
            }
            try
            {
                newGraph.TopologicalSort();
            }
            catch (NonAcyclicGraphException e)
            {
                _logger?.Warn(e, "When update function " + updatingFunc + ", its new calling set form a acyclic graph, thus update don't take effect");
                return false;
            }

            _callingGraph = newGraph;
            
            return true;
        }

        #region Metadata extraction from contract code

        private string ReplacementRegexPattern = @"\$\{[a-zA-Z_][a-zA-Z0-9_]*((\.[a-zA-Z_][a-zA-Z0-9_]*)*)\}";

        /// <summary>
        /// 1. extract attributes in type
        /// 2. check whether this new calling graph is DAG
        /// 3. Return new class's function metadata into map
        /// </summary>
        /// <param name="contractType"></param>
        /// <returns></returns>
        /// <exception cref="FunctionMetadataException"></exception>
        public bool TryAddNewFunctionMetadataFromContractType(Type contractType)
        {
            Dictionary<string, DataAccessMode> localFieldMap = new Dictionary<string, DataAccessMode>();
            Dictionary<string, Type> smartContractReferenceMap = new Dictionary<string, Type>();
            Dictionary<string, FunctionMetadataTemplate> localFunctionMetadataTemplateMap = new Dictionary<string, FunctionMetadataTemplate>();

            var newGraph = _callingGraph.CreateCopy();
            
            
            foreach (var fieldInfo in contractType.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
            {
                var fieldAttr = fieldInfo.GetCustomAttribute<SmartContractFieldDataAttribute>();
                if (fieldAttr == null) continue;
                if (!localFieldMap.TryAdd(fieldAttr.FieldName, fieldAttr.DataAccessMode))
                {
                    throw new FunctionMetadataException("Duplicate name of field attributes in contract " + contractType.Name);
                }
                
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
            
            //check for DAG
            foreach (var functionMetadata in localFunctionMetadataTemplateMap)
            {
                foreach (var callFunc in functionMetadata.Value.CallingSet)
                {
                    if (callFunc.Contains(Replacement.ContractType(contractType)))
                    {
                        newGraph.AddVerticesAndEdge(new Edge<string>(functionMetadata.Key, callFunc));
                    }
                    else
                    {
                        if (newGraph.ContainsVertex(callFunc))
                        {
                            newGraph.AddEdge(new Edge<string>(functionMetadata.Key, callFunc));
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
            
            //merge the function metadata template map
            foreach (var localMetadata in localFunctionMetadataTemplateMap)
            {
                FunctionMetadataTemplateMap.Add(localMetadata.Key, localMetadata.Value);
            }

            foreach (var field in localFieldMap)
            {
                ResourceAccessModes.Add(
                    ReplaceValueIntoReplacement(field.Key, Replacement.This, Replacement.ContractType(contractType)),
                        field.Value);
            }

            foreach (var smartcontractRef in smartContractReferenceMap)
            {
                ReferenceTypes.Add(Replacement.This + "." + smartcontractRef.Key, 
                    smartcontractRef.Value);
            }
            return true;
        }
        
        private string ReplaceValueIntoReplacement(string str, string replacement, string value)
        {
            return str.ReplaceAll(replacement, value);
        }

        private bool TryGetReplacementWithIndex(string str, int index, out string res)
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
        public static readonly string This = "${this}";

        public static string ContractType(Type contractType)
        {
            return "${" + contractType.Name + "}";
        }
    }
}