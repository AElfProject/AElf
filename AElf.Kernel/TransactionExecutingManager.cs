using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Threading;
using System.Linq;
using QuickGraph;
using QuickGraph.Collections;


namespace AElf.Kernel
{
    public class TransactionExecutingManager : ITransactionExecutingManager
    {
        private Mutex mut = new Mutex();
        private Dictionary<IHash, List<ITransaction>> _pending = new Dictionary<IHash, List<ITransaction>>();
        private Dictionary<int, List<IHash>> _executingPlan = new Dictionary<int, List<IHash>>();
        
        
        public Dictionary<IHash, List<ITransaction>> Pending
        {
            get => _pending;
            set => _pending = value;
        }
        public Dictionary<int, List<IHash>> ExecutingPlan { get => _executingPlan;  } 

        
        /// <summary>
        /// AEs the lf. kernel. IT ransaction executing manager. execute async.
        /// </summary>
        /// <returns>The lf. kernel. IT ransaction executing manager. execute async.</returns>
        /// <param name="tx">Tx.</param>
        public async Task ExecuteAsync(ITransaction tx)
        {
            var task = new Task(() =>
            {
                // group transactions by resource type
                var conflicts = tx.GetParallelMetaData().GetDataConflict();
                mut.WaitOne();
                foreach (var res in conflicts)
                {
                    if (_pending[res] != null)
                    {
                        _pending[res] = new List<ITransaction>();
                    }
                    _pending[res].Add(tx);
                    
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
            foreach (var grp in _pending)
            {
                foreach (var tx in grp.Value)
                {
                    if (graph.ContainsVertex(tx.GetHash())) 
                        continue;
                    
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

            //Calculate_executingPlan(graph);
            //AsyncExecuteGraph(graph);
            ColorGraph(graph);
            
            //ConnectedComponentsForColoring(graph);
            
            // reset 
            _pending = new Dictionary<IHash, List<ITransaction>>();
            this.mut.ReleaseMutex();

            // TODO: parallel execution on root nodes;
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
        
        /// <summary>
        /// use coloring algorithm to claasify txs
        /// </summary>
        /// <param name="graph"></param>
        private void ColorGraph(UndirectedGraph<IHash, Edge<IHash>> graph)
        {
            
            // use Max-Root Heap sort to determine coloring order
            BinaryHeap<int, IHash> hashHeap = new BinaryHeap<int, IHash>(MaxIntCompare);
            foreach (var hash in graph.Vertices)
            {
                hashHeap.Add(graph.AdjacentDegree(hash), hash);
            }
            
            // color result for each vertex
            Dictionary<IHash, int> colorResult = new Dictionary<IHash, int>();
            
            // coloring whol graph
            GreedyColoring(graph, hashHeap, colorResult);
            
            foreach (var r in _executingPlan)
            {
                Console.Write(r.Key + ":");
                List<Task> tasks = new List<Task>();
                
                foreach (var h in r.Value)
                {
                    var task = Task.Factory.StartNew(() =>
                    {
                        var a = 1 + 1;
                    });
                    tasks.Add(task);
                }
                Task.WaitAll(tasks.ToArray());
                Console.WriteLine();
            }
            Console.WriteLine();
        }

        /// <summary>
        /// graph coloring algorithm
        /// </summary>
        /// <param name="graph"></param>
        /// <param name="hashHeap"></param>
        /// <param name="colorResult"></param>
        private void GreedyColoring(UndirectedGraph<IHash, Edge<IHash>> graph, BinaryHeap<int, IHash> hashHeap, Dictionary<IHash, int> colorResult)
        {
            
            IHash hash = hashHeap.RemoveMinimum().Value;
            colorResult[hash] = 0;

            if(!_executingPlan.Keys.Contains(0)) _executingPlan[0] = new List<IHash>();
            _executingPlan[0].Add(hash);
            
            // d+1, d means maximum degree in the given graph 
            var maxColorCount = graph.AdjacentDegree(hash) + 1;
            
            // array for colors to represent if available, false == yes, true == no
            var available = new bool[maxColorCount];

            while(hashHeap.Count > 0)
            {
                IHash h = hashHeap.RemoveMinimum().Value;
                
                foreach (var edge in graph.AdjacentEdges(h))
                {
                    var nei = edge.Source != h ? edge.Source : edge.Target;
                    if (colorResult.Keys.Contains(nei) && colorResult[nei] != -1)
                    {
                        available[colorResult[nei]] = true;
                    }
                }

                for (var i = 0; i < maxColorCount; i++)
                {
                    var color = available[i];
                    if (color) 
                        continue;
                    colorResult[h] = i;
                    if(!_executingPlan.Keys.Contains(i)) _executingPlan[i] = new List<IHash>();
                    _executingPlan[i].Add(h);
                    break;
                }
                
                // reset available array, all colors should be available before next iteration
                foreach (var edge in graph.AdjacentEdges(h))
                {
                    var nei = edge.Source != h ? edge.Source : edge.Target;
                    if (colorResult.Keys.Contains(nei)  && colorResult[nei] != -1)
                    {
                        available[colorResult[nei]] = false;
                    }
                }
                
                
            }
            
        }
        
    }
}






















