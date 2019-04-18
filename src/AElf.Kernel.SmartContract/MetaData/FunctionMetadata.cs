using System;
using System.Collections.Generic;
using System.Linq;
using QuickGraph;
using QuickGraph.Algorithms;
using AElf.Kernel.SmartContract.MetaData;

// ReSharper disable once CheckNamespace
namespace AElf.Kernel.SmartContract
{
    public class FunctionMetadataTemplate : IEquatable<FunctionMetadataTemplate>
    {
        public readonly bool TemplateContainsMetadata;

        public FunctionMetadataTemplate(HashSet<string> callingSet, HashSet<Resource> localResourceSet)
        {
            TemplateContainsMetadata = true;
            CallingSet = callingSet;
            LocalResourceSet = localResourceSet;
        }

        /// <summary>
        /// TODO: For the contracts that contains no metadata
        /// </summary>
        /// <param name="noMetadataTemplate"></param>
        public FunctionMetadataTemplate(bool noMetadataTemplate)
        {
            TemplateContainsMetadata = false;
            LocalResourceSet = new HashSet<Resource>
            {
                new Resource("${this}._lock", DataAccessMode.ReadWriteAccountSharing)
            };
            CallingSet = new HashSet<string>();
        }

        public HashSet<string> CallingSet { get; }
        public HashSet<Resource> LocalResourceSet { get; }
        
        bool IEquatable<FunctionMetadataTemplate>.Equals(FunctionMetadataTemplate other)
        {
            return HashSet<string>.CreateSetComparer().Equals(CallingSet, other.CallingSet) &&
                   HashSet<Resource>.CreateSetComparer().Equals(LocalResourceSet, other.LocalResourceSet);
        }
    }

    public class ContractMetadataTemplate
    {
        public List<string> ProcessFunctionOrder;
        public CallGraph LocalCallingGraph;
        public Dictionary<string, FunctionMetadataTemplate> MethodMetadataTemplates;
        public string FullName;
        public Dictionary<string, Address> ContractReferences;
        public Dictionary<string, List<string>> ExternalFuncCall;

        
        public ContractMetadataTemplate(string fullName, Dictionary<string, FunctionMetadataTemplate> methodMetadataTemplates, Dictionary<string, Address> contractReferences)
        {
            FullName = fullName;
            MethodMetadataTemplates = methodMetadataTemplates;
            ContractReferences = contractReferences;
            
            ExternalFuncCall = new Dictionary<string, List<string>>();
            TrySetLocalCallingGraph(out var callGraph, out var topologicRes);
            ProcessFunctionOrder = topologicRes.Reverse().ToList();
            LocalCallingGraph = callGraph;
        }
        
        /// <summary>
        /// try to get
        /// (1) local calling graph, where only local function calls are considered
        /// (2) process function order ( reverse order of topological order of the calling graph)
        /// (3) external function call list for every function
        /// </summary>
        /// <param name="callGraph"></param>
        /// <param name="topologicRes"></param>
        private void TrySetLocalCallingGraph(out CallGraph callGraph, out IEnumerable<string> topologicRes)
        {
            callGraph = new CallGraph();
            foreach (var kvPair in MethodMetadataTemplates)
            {
                callGraph.AddVertex(kvPair.Key);
                foreach (var calledFunc in kvPair.Value.CallingSet)
                { 
                    if (calledFunc.StartsWith(Replacement.This))
                    {
                        callGraph.AddVerticesAndEdge(new Edge<string>(kvPair.Key, calledFunc));
                    }
                    else
                    {
                        if (!ExternalFuncCall.TryGetValue(kvPair.Key, out var callingList))
                        {
                            callingList = new List<string>();
                            ExternalFuncCall.Add(kvPair.Key, callingList);
                        }
                        callingList.Add(calledFunc);
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
                throw new FunctionMetadataException($"Calling graph of contract {FullName} is Non-DAG thus nothing take effect");
            }
        }
    }

    
}