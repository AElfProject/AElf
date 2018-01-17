using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Threading;
using System.Diagnostics.Contracts;
using System.Drawing;
using System.IO;
using System.Linq;
using AElf.Kernel.Worker;
using QuickGraph;
using QuickGraph.Collections;

namespace AElf.Kernel
{
    public class TransactionExecutingManager : ITransactionExecutingManager
    {
        private Mutex mut = new Mutex();
        private Dictionary<IHash, List<ITransaction>> pending = new Dictionary<IHash, List<ITransaction>>();
        public static Dictionary<int, List<IHash>> ExecutePlan = new Dictionary<int, List<IHash>>();
        

        public TransactionExecutingManager()
        {
        }

        
        
        public Dictionary<IHash, List<ITransaction>> Pending
        {
            get => pending;
            set => pending = value;
        }

        
        /// <summary>
        /// AEs the lf. kernel. IT ransaction executing manager. execute async.
        /// </summary>
        /// <returns>The lf. kernel. IT ransaction executing manager. execute async.</returns>
        /// <param name="tx">Tx.</param>
        async Task ITransactionExecutingManager.ExecuteAsync(ITransaction tx)
        {
            Task task = new Task(() =>
            {
                // group transactions by resource type
                var conflicts = tx.GetParallelMetaData().GetDataConflict();
                mut.WaitOne();
                foreach (var res in conflicts)
                {
                    if (pending[res] != null)
                    {
                        pending[res] = new List<ITransaction>();
                    }
                    pending[res].Add(tx);
                    
                }
                mut.ReleaseMutex();
            });
            task.Start();

            await task;
        }

        

        /// <summary>
        /// Schedule execution of transaction
        /// </summary>
        public void Schedule()
        {
            //  Execution strategy(experimental)
            //  1. tranform the dependency of Resource(R) into graph of related Transactions(T)
            //  2. find the T(ransaction) which connects to the most neightbours
            //  3. execute the T(ransaction), and removes this node from the graph
            //  4. check to see if this removal leads to graph split
            //  5. if YES, we can parallel execute the transactions from the splitted graph
            //  6  if NO, goto step 2

            // build the graph
            UndirectedGraph<IHash, Edge<IHash>> graph = new UndirectedGraph<IHash, Edge<IHash>>(false);
            
            this.mut.WaitOne();
            foreach (var grp in pending)
            {
                foreach (var tx in grp.Value)
                {
                    if (graph.ContainsVertex(tx.GetHash())) continue;
                    graph.AddVertex(tx.GetHash());
                }

                foreach (var tx in grp.Value)
                {
                    foreach (var neighbour in grp.Value)
                    {
                        if (!tx.Equals(neighbour))
                        {
                            graph.AddEdge(new Edge<IHash>(tx.GetHash(), neighbour.GetHash()));
                            
                        }
                    }
                }
            }

            CalculateExecutingPlan(graph);
            AsyncExecuteGraph(graph);
            // reset 
            pending = new Dictionary<IHash, List<ITransaction>>();
            this.mut.ReleaseMutex();

            // TODO: parallel execution on root nodes;
        }


        /// <summary>
        /// Executes the graph synchronously
        /// </summary>
        /// <param name="n">N.</param>
        /// <param name="phase">phase</param>
        private void ExecuteGraph(UndirectedGraph<IHash, Edge<IHash>> n, int phase)
        {
            
            /*
             * 1. DFS is applied to traverse graph to find the node with most neighbors for each subgraphs.
             * 2. use Hash-Graph map and Max-root heap to maintain subGraphs processing in turn
             * 3. Repeat above process until all graph done.
             * 4. use bipartite check to accelerate processing(Black-White graph)
             */
            
            // Max-Root Heap
            BinaryHeap<int, IHash> hashHeap = new BinaryHeap<int, IHash>(MaxIntCompare);
            
            // hashTograph map
            Dictionary<IHash,UndirectedGraph<IHash, Edge<IHash>>> hashToGraph= new Dictionary<IHash, UndirectedGraph<IHash, Edge<IHash>>>();

            // verify graph connectivity and map hash to subgraphs
            SubGraphsWithMaxHeap(n, hashHeap, hashToGraph);

            while(hashHeap.Count>0)
            {
                var hashToProcess = hashHeap.RemoveMinimum().Value;
               
                var subgraph = hashToGraph[hashToProcess];
                
                //TODO: process the sigle task synchronously
                TPLWorker tplWorker=new TPLWorker();
                
                subgraph.RemoveVertex(hashToProcess);

                SubGraphsWithMaxHeap(subgraph, hashHeap, hashToGraph);
                hashToGraph.Remove(hashToProcess);
            }
        }

        
        
        /// <summary>
        /// calculate the executing plan recorded in ExecutePlan
        /// </summary>
        /// <param name="n"> graph </param>
        private void CalculateExecutingPlan(UndirectedGraph<IHash, Edge<IHash>> graph)
        {
            ExecutePlan = new Dictionary<int, List<IHash>>();
            int phase = 0;
            Dictionary<UndirectedGraph<IHash, Edge<IHash>>, IHash> graphToHash = new Dictionary<UndirectedGraph<IHash, Edge<IHash>>, IHash>();
            if (graph.VertexCount > 0)
                graphToHash[graph] = graph.Vertices.ElementAt(0);
            GeneratePhase(graphToHash, phase);
        }
        
        

        /// <summary>
        /// verify graph connectivity and get all connected components
        /// </summary>
        /// <param name="graph"></param>
        /// <param name="graphToHash"></param>
        private void SubGraphs(UndirectedGraph<IHash, Edge<IHash>> graph, Dictionary<UndirectedGraph<IHash, Edge<IHash>>, IHash>  graphToHash)
        {

            // Bipartite Graph check
            Dictionary<IHash,int> colorDictionary = new Dictionary<IHash, int>();
            
            foreach (var hash in graph.Vertices)
            {
                if (colorDictionary.Keys.Contains(hash)) continue;
                
                UndirectedGraph<IHash, Edge<IHash>> subGraph = new UndirectedGraph<IHash, Edge<IHash>>();
                
                // hash with most dependencies in the graph 
                IHash maxHash = hash;
                DfsSearch(graph, subGraph, ref maxHash, colorDictionary);
                
                graphToHash[subGraph] = maxHash;
                
            }

        }

        /// <summary>
        /// generate txs can be executed in one phase
        /// </summary>
        /// <param name="graphToHash"></param>
        /// <param name="phase"></param>
        private void GeneratePhase(Dictionary<UndirectedGraph<IHash, Edge<IHash>>, IHash>  graphToHash, int phase)
        {
            Dictionary<UndirectedGraph<IHash, Edge<IHash>>, IHash> nextPhaseGrapthToHash = new Dictionary<UndirectedGraph<IHash, Edge<IHash>>, IHash>();
            
            foreach (var graph in graphToHash.Keys)
            {
                if (graph.VertexCount == 0) continue;
                SubGraphs(graph, nextPhaseGrapthToHash);
            }
            
            foreach (var graph in nextPhaseGrapthToHash.Keys)
            {
                if(!ExecutePlan.Keys.Contains(phase)) ExecutePlan[phase] = new List<IHash>();
                ExecutePlan[phase].Add(nextPhaseGrapthToHash[graph]);

                graph.RemoveVertex(nextPhaseGrapthToHash[graph]);
            }
            
            graphToHash = nextPhaseGrapthToHash;
            
            if (graphToHash.Count == 0) return;
            GeneratePhase(graphToHash, phase+1);

        }

        /// <summary>
        /// Executes the graph asynchronously
        /// </summary>
        /// <param name="n"></param>
        /// <returns></returns>
        private void AsyncExecuteGraph(UndirectedGraph<IHash, Edge<IHash>> n)
        {
            /*
             * search for subgraphs and process subgraphs asynchronously
             * if no subgraphs, execute and remove the node with most neighbors
             */

            Dictionary<IHash, int> colorDictionary = new Dictionary<IHash, int>();

            List<Task> tasks=new List<Task>();
            foreach (var hash in n.Vertices)
            {
                if (colorDictionary.Keys.Contains(hash)) continue;
                
                UndirectedGraph<IHash, Edge<IHash>> subGraph = new UndirectedGraph<IHash, Edge<IHash>>();
                IHash maxHash = hash;
                bool isBipartite = DfsSearch(n, subGraph, ref maxHash, colorDictionary);

                if (isBipartite)
                {
                    //TODO : if bipartite, parallel process for tasks in both sets asynchronously;
                    /*foreach (var h in subGraph.Vertices)
                    {
                        if (colorDictionary[h]==1)
                        {
                            Console.Write("T"+Thread.CurrentThread.ManagedThreadId+":" + (char)h.GetHashBytes()[0]+"!    ");
                        }
                        if (colorDictionary[h]==-1)
                        {
                            Console.Write("T"+Thread.CurrentThread.ManagedThreadId+":" + (char)h.GetHashBytes()[0]+"?    ");
                        }
                    }
                    continue;*/
                }

                //if not Bipartite, execute ths subgraph in new task
                Task task = Task.Run(() =>
                {
                    //process the tx
                    
                    subGraph.RemoveVertex(maxHash); 
                    AsyncExecuteGraph(subGraph);
                });
                tasks.Add(task);
            }
            
            Task.WaitAll(tasks.ToArray());
        }

        
        
        
        /// <summary>
        /// verify graph connectivity for synchronously process
        /// </summary>
        /// <param name="n"></param>
        /// <param name="hashHeap"></param>
        /// <param name="hashToGraph"></param>
        private void SubGraphsWithMaxHeap(UndirectedGraph<IHash, Edge<IHash>> n, BinaryHeap<int, IHash> hashHeap, Dictionary<IHash, UndirectedGraph<IHash, Edge<IHash>>> hashToGraph)
        {
            // Bipartite Graph check
            Dictionary<IHash,int> colorDictionary = new Dictionary<IHash, int>();
            
            foreach (var hash in n.Vertices)
            {
                if (colorDictionary.Keys.Contains(hash)) continue;
                
                UndirectedGraph<IHash, Edge<IHash>> subGraph = new UndirectedGraph<IHash, Edge<IHash>>();
                
                // hash with most dependencies in the graph 
                IHash maxHash = hash;
                bool isBipartite  = DfsSearch(n, subGraph, ref maxHash, colorDictionary);
                
                
                if (isBipartite)
                {
                    //TODO : if bipartite, parallel process for tasks in both sets asynchronously;
                    /*foreach (var h in subGraph.Vertices)
                    {
                        if (colorDictionary[h] == 1)
                        {
                            Console.Write("white:" + (char)h.GetHashBytes()[0]+"    ");
                        }
                        if (colorDictionary[h] == -1)
                        {
                            Console.Write("black:" + (char)h.GetHashBytes()[0]+"    ");
                        }
                       
                    }
                    continue;*/
                }
                
                //if not Bipartite, add maxhash to heap and hashToGraph Dictionary
                hashHeap.Add(subGraph.AdjacentDegree(maxHash),maxHash);
                hashToGraph[maxHash]=subGraph;
                
            }
            
        }
        
        /// <summary>
        /// DFS is applied to traverse graph with color for bipartite  
        /// </summary>
        /// <param name="n">N.</param>
        /// <param name="subGraph" />
        /// <param name="maxHash"></param>
        /// <param name="colorDictionary"></param>
        private bool DfsSearch(UndirectedGraph<IHash, Edge<IHash>> n,  UndirectedGraph<IHash, Edge<IHash>> subGraph, ref IHash maxHash, Dictionary<IHash,int> colorDictionary)
        {
            //stack
            Stack<IHash> stack=new Stack<IHash>();
            stack.Push(maxHash);
            subGraph.AddVertex(maxHash);
            
            int color = 1;
            colorDictionary[maxHash] = color;
            bool res = true;
            
            
            while (stack.Count>0)
            {
                IHash cur = stack.Pop();
                
                maxHash = n.AdjacentDegree(maxHash) > n.AdjacentDegree(cur) ? maxHash : cur;

                //opposite color
                color = colorDictionary[cur] * -1;

                foreach (var edge in n.AdjacentEdges(cur))
                {
                    IHash nei = edge.Source == cur ? edge.Target : edge.Source;
                    
                    //color check 
                    if (colorDictionary.Keys.Contains(nei))
                    {
                        if (colorDictionary[nei] != color) res = false;
                    }
                    else
                    {
                        //add vertex 
                        subGraph.AddVertex(nei);
                        colorDictionary.Add(nei, color);
                        stack.Push(nei);
                    }
                    
                    //add edge
                    if(!subGraph.ContainsEdge(edge)) subGraph.AddEdge(edge);
                }
            }
            
            return res;
            
        }
        
        /// <summary>
        /// comparsion for heap
        /// </summary>
        /// <param name="i1"></param>
        /// <param name="i2"></param>
        /// <returns></returns>
        int MaxIntCompare(int i1, int i2)
        {
            if (i1 < i2)
                return 1;     
            if (i1 > i2)
                return -1;
            return 0;
        }
    }
}
