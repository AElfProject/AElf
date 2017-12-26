using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Threading;
using System.Diagnostics.Contracts;
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
        /// Parallel Executes the graph
        /// </summary>
        /// <param name="n">N.</param>
        public void ExecuteGraph(UndirectedGraph<IHash, Edge<IHash>> n)
        {
            
            // Bipartite Graph check
            Dictionary<IHash,int> colorDictionary = new Dictionary<IHash, int>();

            
            foreach (var ihash in n.Vertices)
            {
                if (colorDictionary.Keys.Contains(ihash)) continue;

                UndirectedGraph<IHash, Edge<IHash>> subGraph = new UndirectedGraph<IHash, Edge<IHash>>();
                BinaryHeap<int, IHash> binaryHeap = new BinaryHeap<int, IHash>(MaxIntCompare);

                // dfs search for connectivity and create heap for subgraph
                
                bool bipartite = DfsSearch(n, ihash,  subGraph, binaryHeap, colorDictionary);
                

                if (bipartite)
                {
                    Console.WriteLine("bipartite");
                    
                    /*foreach (var hash in subGraph.Vertices)
                    {
                        
                    }*/
                    
                    //TODO : parallel process for tasks in both sets asynchronously;
                    
                    continue;
                }
                

                //if not bipartite, continue excute subgraphs
                //remove heap root and vertex from graph
                var txIhash = binaryHeap.RemoveMinimum().Value;

                if (subGraph.VertexCount == 1)
                {
                    //TODO: if only one task, process single task asynchronously,
                    continue;
                }
                //TODO: if more than one task, process single task synchronously,
                
                subGraph.RemoveVertex(txIhash);
                Console.WriteLine("remove:"+ txIhash.GetHashBytes()[0]);

                if (subGraph.VertexCount > 1)
                    ExecuteGraph(subGraph);
            }
        }


        /// <summary>
        /// dfs and add vertexs to heap during search,
        /// heap root is the vertex with most neighbors 
        /// </summary>
        /// <param name="n">N.</param>
        /// <param name="ihash"></param>
        /// <param name="subGraph" />
        /// <param name="binaryHeap"></param>
        /// <param name="colorDictionary"></param>
        bool DfsSearch(UndirectedGraph<IHash, Edge<IHash>> n, IHash ihash, UndirectedGraph<IHash, Edge<IHash>> subGraph, BinaryHeap<int,IHash> binaryHeap,  Dictionary<IHash,int> colorDictionary)
        {

            //stack
            Stack<IHash> stack=new Stack<IHash>();
            stack.Push(ihash);
            subGraph.AddVertex(ihash);
            binaryHeap.Add(n.AdjacentEdges(ihash).Count(),ihash);
            int color = 1;
            colorDictionary[ihash] = color;
            bool res = true;

            while (stack.Count>0)
            {
                

                IHash cur = stack.Pop();
                
                // add task ihash to heap when pop
                binaryHeap.Add(n.AdjacentEdges(cur).Count(),cur);

                //opposite color
                color = colorDictionary[cur] * -1;
                //Console.Write(cur.GetHashBytes()[0]+" ");
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
                        
                        colorDictionary.Add(nei,color);
                        stack.Push(nei);
                    }
                    //add edge
                    if(!subGraph.ContainsEdge(edge)) subGraph.AddEdge(edge);
                }
            }
            //Console.WriteLine();
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
