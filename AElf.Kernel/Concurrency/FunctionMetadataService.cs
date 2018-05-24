using System;
using System.Collections.Generic;
using System.Linq;
using Org.BouncyCastle.Security;
using QuickGraph;
using QuickGraph.Algorithms;
using ServiceStack;

namespace AElf.Kernel.Concurrency
{
    /// <summary>
    /// Where get and set the metadata when deploy the contracts and check correctness when trying to updating contracts(functions)
    /// TODO: currently just sopport update one function of a contract, if trying to update multiple function at a time, the calling graph and the FunctionMetadataMap should be backup before the update in case one of the function fail the update and all the preceding updated function need to roll back their effect. Or maybe just check Whether applying all the updating functions can result in non-DAG calling graph. 
    /// </summary>
    public class FunctionMetadataService : IFunctionMetaDataService
    {
        public Dictionary<string, FunctionMetadata> FunctionMetadataMap { get; } = new Dictionary<string, FunctionMetadata>();
        private AdjacencyGraph<string, Edge<string>> _callingGraph;

        public FunctionMetadataService()
        {
            _callingGraph = new AdjacencyGraph<string, Edge<string>>();
        }


        public bool SetNewFunctionMetadata(string functionFullName, HashSet<string> otherFunctionsCallByThis, HashSet<Hash> nonRecursivePathSet)
        {
            if (FunctionMetadataMap.ContainsKey(functionFullName))
            {
                //This should be the completely new function
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

            if (!TryUpdateCallingGraph(functionFullName, oldMetadata.CallingSet, newOtherFunctionsCallByThis))
            {
                //new graph have circle, nothing take effect
                return false;
            }

            FunctionMetadataMap.Remove(functionFullName);
            
            if(!SetNewFunctionMetadata(functionFullName, newOtherFunctionsCallByThis, newNonRecursivePathSet)){
                //This should be unReachable, because function above already check whether new graph is DAG
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
                Console.WriteLine("FunctionMetadataService: When update function " + updatingFunc + ", its new calling set form a acyclic graph, thus update don't take effect");
                return false;
            }

            _callingGraph = newGraph;
            
            return true;
        }
    }
}