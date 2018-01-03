using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Threading;
using System.Diagnostics.Contracts;
using System.Drawing;
using System.Linq;
using QuickGraph;
using QuickGraph.Collections;

namespace AElf.Kernel
{
    public class TransactionExecutingManager : ITransactionExecutingManager
    {
        private Mutex mut = new Mutex();
        private Dictionary<IHash, List<ITransaction>> pending = new Dictionary<IHash, List<ITransaction>>();

        public TransactionExecutingManager()
        {
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
                this.mut.WaitOne();
                foreach (var res in conflicts)
                {
                    if (pending[res] != null)
                    {
                        pending[res] = new List<ITransaction>();

                    }
                    pending[res].Add(tx);
                    
                }
                this.mut.ReleaseMutex();
            });
            task.Start();

            await task;
        }

        /// <summary>
        /// Schedule execution of transaction
        /// </summary>
        void Scheduler()
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

            ExecuteGraph(graph);
            // reset 
            pending = new Dictionary<IHash, List<ITransaction>>();
            this.mut.ReleaseMutex();

            // TODO: parallel execution on root nodes;
        }


        /// <summary>
        /// Executes the graph
        /// 
        /// </summary>
        /// <param name="n">N.</param>
        private void ExecuteGraph(UndirectedGraph<IHash, Edge<IHash>> n)
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
            SubGraphs(n,hashHeap,hashToGraph);

            while(hashHeap.Count>0)
            {
                var hashToProcess = hashHeap.RemoveMinimum().Value;
               
                var subgraph = hashToGraph[hashToProcess];
                
                //TODO: process the sigle task synchronously
                
                
                subgraph.RemoveVertex(hashToProcess);

                SubGraphs(subgraph, hashHeap, hashToGraph);
                hashToGraph.Remove(hashToProcess);
            }
        }

        /// <summary>
        /// Executes the graph asynchronously
        /// </summary>
        /// <param name="n"></param>
        /// <returns></returns>
        public async Task AsyncExecuteGraph(UndirectedGraph<IHash, Edge<IHash>> n)
        {
            
            /*
             * 1. DFS is applied to traverse graph to execute and remove the node with most neighbors Synchronously.
             * 2. Repeat above step 1 until the graph is split apart and goto step 3.
             * 3. Process each subgraph Asynchronously with new task.
             */
            
            Task task=Task.Run( () =>
            {
                // hashTograph map
                Dictionary<IHash,UndirectedGraph<IHash, Edge<IHash>>> hashToGraph= new Dictionary<IHash, UndirectedGraph<IHash, Edge<IHash>>>(); 

                // verify graph connectivity and map hash to subgraphs
                SubGraphsForAsync(n,hashToGraph);
            
                //remove nodes until the graph is split
                while(hashToGraph.Count==1)
                {
                    var hashToProcess = hashToGraph.Keys.ElementAt(0);
                    var graph = hashToGraph[hashToProcess];
                
                    //TODO: process the sigle task synchronously
                
                    
                    graph.RemoveVertex(hashToProcess);
                    hashToGraph.Remove(hashToProcess);
                
                    SubGraphsForAsync(graph,hashToGraph);
                }
                
                // Asynchronously process subgraphs
                foreach (var hash in hashToGraph.Keys)
                {
                    Task t= AsyncExecuteGraph(hashToGraph[hash]);
                }
            });
            
            task.Start();
            
            await task;
        }

        
        /// <summary>
        /// verify graph connectivity for synchronously process
        /// </summary>
        /// <param name="n"></param>
        /// <param name="hashHeap"></param>
        /// <param name="hashToGraph"></param>
        private void SubGraphs(UndirectedGraph<IHash, Edge<IHash>> n, BinaryHeap<int, IHash> hashHeap, Dictionary<IHash,UndirectedGraph<IHash, Edge<IHash>>> hashToGraph)
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
                    foreach (var h in subGraph.Vertices)
                    {
                        if (colorDictionary[h]==1)
                        {
                            Console.Write("white:" + (char)h.GetHashBytes()[0]+"    ");
                        }
                        if (colorDictionary[h]==-1)
                        {
                            Console.Write("black:" + (char)h.GetHashBytes()[0]+"    ");
                        }
                       
                    }
                    continue;
                }
                
                //if not Bipartite, add maxhash to heap and hashToGraph Dictionary
                hashHeap.Add(subGraph.AdjacentDegree(maxHash),maxHash);
                hashToGraph[maxHash]=subGraph;
                
            }
            
        }
        
        /// <summary>
        /// verify graph connectivity for asynchronously process
        /// </summary>
        /// <param name="n"></param>
        /// <param name="hashToGraph"></param>
        private void SubGraphsForAsync(UndirectedGraph<IHash, Edge<IHash>> n,  Dictionary<IHash,UndirectedGraph<IHash, Edge<IHash>>> hashToGraph)
        {
            // Bipartite Graph check
            Dictionary<IHash,int> colorDictionary = new Dictionary<IHash, int>();
            
            foreach (var hash in n.Vertices)
            {
                if (colorDictionary.Keys.Contains(hash)) continue;
                
                UndirectedGraph<IHash, Edge<IHash>> subGraph = new UndirectedGraph<IHash, Edge<IHash>>();
                IHash initIHash = hash;
                bool isBipartite  = DfsSearch(n, subGraph, ref initIHash, colorDictionary);
                
                /*
                if (isBipartite)
                {
                    //TODO : if bipartite, parallel process for tasks in both sets asynchronously;
                    foreach (var h in subGraph.Vertices)
                    {
                        if (colorDictionary[h]==1)
                        {
                            Console.Write("Thread "+Thread.CurrentThread.ManagedThreadId+" white:" + (char)h.GetHashBytes()[0]+"    ");
                        }
                        if (colorDictionary[h]==-1)
                        {
                            Console.Write("Thread "+Thread.CurrentThread.ManagedThreadId+" black:" + (char)h.GetHashBytes()[0]+"    ");
                        }
                       
                    }
                    continue;
                }*/
                
                //if not Bipartite, add maxhash to hashToGraph Dictionary
                hashToGraph[initIHash]=subGraph;
                
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
