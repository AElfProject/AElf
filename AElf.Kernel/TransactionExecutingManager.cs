﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using AElf.Kernel.Extensions;
using AElf.Kernel.KernelAccount;
using QuickGraph;


namespace AElf.Kernel
{
    public class TransactionExecutingManager : ITransactionExecutingManager
    {
        
        public Dictionary<int, List<ITransaction>> ExecutingPlan { get; private set; }
        private Dictionary<IAccount, List<ITransaction>> _pending;
        private UndirectedGraph<ITransaction, Edge<ITransaction>> _graph;
        private readonly ISmartContractManager _smartContractManager;

        public TransactionExecutingManager(ISmartContractManager smartContractManager)
        {
            _smartContractManager = smartContractManager;
        }
        
        
        /// <summary>
        /// AElf.kernel.ITransaction executing manager. execute async.
        /// </summary>
        /// <returns>The lf. kernel. IT ransaction executing manager. execute async.</returns>
        /// <param name="tx">Tx.</param>
        public async Task ExecuteAsync(ITransaction tx)
        {
            var smartContract = await _smartContractManager.GetAsync(tx.To);
            await smartContract.InvokeAsync(tx.From.GetAddress(), tx.MethodName, tx.Params);
        }
        
        
        
        /// <summary>
        /// Schedule execution of transaction
        /// </summary>
        public void Schedule(List<ITransaction> transactions)
        {
            // reset 
            _pending = new Dictionary<IAccount, List<ITransaction>>();
            ExecutingPlan = new Dictionary<int, List<ITransaction>>();
            _graph = new UndirectedGraph<ITransaction, Edge<ITransaction>>(false);
            
            foreach (var tx in transactions)
            {
                var conflicts = new List<IAccount> {tx.From, tx.To};
                _graph.AddVertex(tx);
                foreach (var res in conflicts)
                {
                    if (!_pending.ContainsKey(res))
                    {
                        _pending[res] = new List<ITransaction>();
                    }
                    foreach (var t in _pending[res])
                    {
                        _graph.AddEdge(new Edge<ITransaction>(t, tx));
                    }
                    _pending[res].Add(tx);
                }
            }
            ColorGraph(transactions); 
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
        private void ColorGraph(List<ITransaction> transactions)
        {
            // color result for each vertex
            Dictionary<ITransaction, int> colorResult = new Dictionary<ITransaction, int>();
            
            // coloring whol graph
            GreedyColoring(colorResult, transactions);
            foreach (var r in ExecutingPlan)
            {
                List<Task> tasks = new List<Task>();
                
                foreach (var h in r.Value)
                {
                    var task = ExecuteAsync(h);
                    tasks.Add(task);
                }
                Task.WaitAll(tasks.ToArray());
            }
        }

        

        /// <summary>
        /// graph coloring algorithm
        /// </summary>
        /// <param name="colorResult"></param>
        /// <param name="transactions"></param>
        private void GreedyColoring(Dictionary<ITransaction, int> colorResult, List<ITransaction> transactions)
        {
            // array for colors to represent if available, false == yes, true == no
            var available = new List<bool> {false};
            foreach (var tx in  transactions)
            {
                foreach (var edge in _graph.AdjacentEdges(tx))
                {
                    var nei = edge.Source != tx ? edge.Source : edge.Target;
                    if (colorResult.Keys.Contains(nei) && colorResult[nei] != -1)
                    {
                        available[colorResult[nei]] = true;
                    }
                }
                
                var i = 0;
                for (; i < available.Count; i++)
                {
                    if (available[i]) 
                        continue;
                    colorResult[tx] = i;
                    if(!ExecutingPlan.Keys.Contains(i)) ExecutingPlan[i] = new List<ITransaction>();
                    ExecutingPlan[i].Add(tx);
                    break;
                }
                if (i == available.Count)
                {
                    available.Add(false);
                    colorResult[tx] = i;
                    if(!ExecutingPlan.Keys.Contains(i)) ExecutingPlan[i] = new List<ITransaction>();
                    ExecutingPlan[i].Add(tx);
                }
                
                // reset available array, all colors should be available before next iteration
                for (int j = 0; j < available.Count; j++)
                {
                    available[j] = false;
                }
            }
        }
        
    }
}






















