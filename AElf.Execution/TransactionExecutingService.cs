using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using AElf.Kernel.Types;
using QuickGraph;
using AElf.Kernel;
using AElf.SmartContract;
using AElf.Common;

namespace AElf.Execution
{
    public class TransactionExecutingService : ITransactionExecutingService
    {
        public Dictionary<int, List<Transaction>> ExecutingPlan { get; private set; }
        private Dictionary<Address, List<Transaction>> _pending;
        private UndirectedGraph<Transaction, Edge<Transaction>> _graph;
        private readonly ISmartContractService _smartContractService;
        private readonly IChainContext _chainContext;

        public TransactionExecutingService(ISmartContractService smartContractService, IChainContext chain)
        {
            _smartContractService = smartContractService;
            _chainContext = chain;
        }


        /// <summary>
        /// AElf.kernel.Transaction executing service. execute async.
        /// </summary>
        /// <returns>The lf. kernel. IT ransaction executing manager. execute async.</returns>
        /// <param name="tx">Tx.</param>
        /// <param name="chain"></param>
        public async Task ExecuteAsync(Transaction tx)
        {
            // TODO: *** Contract Issues ***
            //var smartContract = await _smartContractService.GetAsync(tx.To, _chainContext);
            
            //var context = new SmartContractInvokeContext()
            //{
            //    Caller = tx.From,
            //    IncrementId = tx.IncrementId,
            //    MethodName = tx.MethodName,
            //    Params = tx.Params
            //};
            
            //await smartContract.InvokeAsync(context);
        }

        
        /// <summary>
        /// Schedule execution of transaction
        /// </summary>
        public void Schedule(List<Transaction> transactions, IChainContext chainContext)
        {
            // reset 
            _pending = new Dictionary<Address, List<Transaction>>();
            ExecutingPlan = new Dictionary<int, List<Transaction>>();
            _graph = new UndirectedGraph<Transaction, Edge<Transaction>>(false);
            
            foreach (var tx in transactions)
            {
                var conflicts = new List<Address> {tx.From, tx.To};
                _graph.AddVertex(tx);
                foreach (var res in conflicts)
                {
                       
                    if (!_pending.ContainsKey(res))
                    {
                        _pending[res] = new List<Transaction>();
                    }
                    foreach (var t in _pending[res])
                    {
                        _graph.AddEdge(new Edge<Transaction>(t, tx));
                    }
                    _pending[res].Add(tx);
                }
            }
            ColorGraph(transactions, chainContext); 
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
        private void ColorGraph(List<Transaction> transactions, IChainContext chainContext)
        {
            // color result for each vertex
            Dictionary<Transaction, int> colorResult = new Dictionary<Transaction, int>();
            
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
        private void GreedyColoring(Dictionary<Transaction, int> colorResult, List<Transaction> transactions)
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
                    if(!ExecutingPlan.Keys.Contains(i)) ExecutingPlan[i] = new List<Transaction>();
                    ExecutingPlan[i].Add(tx);
                    break;
                }
                if (i == available.Count)
                {
                    available.Add(false);
                    colorResult[tx] = i;
                    if(!ExecutingPlan.Keys.Contains(i)) ExecutingPlan[i] = new List<Transaction>();
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