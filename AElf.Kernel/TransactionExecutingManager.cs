using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using AElf.Kernel.Extensions;
using QuickGraph;


namespace AElf.Kernel
{
    public class TransactionExecutingManager : ITransactionExecutingManager
    {
        
        private readonly object _locker = new object();
        private Dictionary<IAccount, List<ITransaction>> _pending;
        private UndirectedGraph<ITransaction, Edge<ITransaction>> _graph;
        public Dictionary<int, List<ITransaction>> ExecutingPlan { get; private set; }
        private readonly WorldState _worldState;

        public TransactionExecutingManager(WorldState worldState)
        {
            _worldState = worldState;
        }
        
        
        /// <summary>
        /// AElf.kernel.ITransaction executing manager. execute async.
        /// </summary>
        /// <returns>The lf. kernel. IT ransaction executing manager. execute async.</returns>
        /// <param name="tx">Tx.</param>
        public Task ExecuteAsync(ITransaction tx)
        {
            
            var task = Task.Factory.StartNew(() =>
            {
                try
                {
                    var a = 1 + 1;
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                    throw;
                }
            });
            return task;
        }


        /// <summary>
        /// transfer coins between accounts
        /// <param name="tx"></param>
        /// </summary>
        private void Transfer(ITransaction tx)
        {
            var accountFrom = tx.From;
            var accountTo = tx.To;
            var methodName = tx.MethodName;
                    
            var accountFromDataProvider = _worldState.GetAccountDataProviderByAccount(accountFrom);
            var accountToDataProvider = _worldState.GetAccountDataProviderByAccount(accountTo);
            
            var param = tx.Params;
            if (param.Length != 1 || (int)param[0] < 0)
                throw new ArgumentException("Illegal parameter", "params");
                        
            var fromBalanceHash = accountFrom.CalculateHashWith("Balance");
            var fromBalanceDataProvider = accountFromDataProvider.GetDataProvider().GetDataProvider("Balance");
            var fromBalance = fromBalanceDataProvider.GetAsync(new Hash<decimal>(fromBalanceHash)).Result;
            
            var toBalanceHash = accountTo.CalculateHashWith("Balance");
            var toBalanceDataProvider = accountToDataProvider.GetDataProvider().GetDataProvider("Balance");
            var toBalance = toBalanceDataProvider.GetAsync(new Hash<decimal>(toBalanceHash)).Result;
            
            // TODO: deserialize the Balances
            
            
            // TODO: calculate
            decimal amount = (decimal) param[0];

            // TODO: serialize new Balances and uodate
            
            //accountFromDataProvider.GetDataProvider().GetDataProvider("Balance").SetAsync((accountFrom.CalculateHash(), );
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






















