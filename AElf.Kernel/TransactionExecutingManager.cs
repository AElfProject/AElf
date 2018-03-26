using System;
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
        private readonly WorldState _worldState;
        private readonly AccountZero _accountZero;
        public Dictionary<int, List<ITransaction>> ExecutingPlan { get; private set; }
        private Dictionary<IAccount, List<ITransaction>> _pending;
        private UndirectedGraph<ITransaction, Edge<ITransaction>> _graph;
        private readonly ISmartContractService _smartContractManager;

        public TransactionExecutingManager(WorldState worldState, AccountZero accountZero,
            ISmartContractService smartContractManager)
        {
            _worldState = worldState;
            _accountZero = accountZero;
            _smartContractManager = smartContractManager;
        }


        /// <summary>
        /// AElf.kernel.ITransaction executing manager. execute async.
        /// </summary>
        /// <returns>The lf. kernel. IT ransaction executing manager. execute async.</returns>
        /// <param name="tx">Tx.</param>
        /// <param name="chain"></param>
        public async Task ExecuteAsync(ITransaction tx, IChainContext chain)
        {
            var smartContract = await _smartContractManager.GetAsync(tx.To.GetAddress(), chain);

            await smartContract.InvokeAsync(tx.From.GetAddress(), tx.MethodName, tx.Params);
        }

//            
//            var task = Task.Factory.StartNew(async () =>
//            {
//                // TODO: execute tx and  exceptions handling
//                var method = tx.MethodName;
//                var accountFrom = tx.From;
//                var accountTo = tx.To;
//                var param = tx.Params;
//                
//                switch (method)
//                {
//                    case "transfer":
//                        await Transfer(accountFrom, accountTo, (decimal) param.ElementAt(0));
//                        break;
//                    case "CreatAccount":
//                        await CreateAccount(accountFrom, (string) param.ElementAt(0));
//                        break;
//                    case "InvokeMethod":
//                        await InvokeMethod(accountFrom, accountTo, (string) param.ElementAt(0),
//                            (object[]) param.ElementAt(1));
//                        break;
//                    case "DeployContract":
//                        await DeploySmartContract(accountFrom, (int) param.ElementAt(0), (string) param.ElementAt(1),
//                            (byte[]) param.ElementAt(2));
//                        break;
//                    default:
//                        Console.WriteLine("Default case");
//                        break;
//                }
//            });
//        }

        /*

        /// <summary>
        /// transfer coins between accounts
        /// </summary>
        /// <param name="accountFrom"></param>
        /// <param name="accountTo"></param>
        /// <param name="amount"></param>
        private async Task Transfer(IAccount accountFrom, IAccount accountTo, decimal amount)
        {
            // get accountDataProviders from WorldState
            var accountFromDataProvider = _worldState.GetAccountDataProviderByAccount(accountFrom);
            var accountToDataProvider = _worldState.GetAccountDataProviderByAccount(accountTo);
            
            // use dataProvider to get Serialized Balance obj
            var fromBalanceHash = new Hash<decimal>(accountFrom.CalculateHashWith("Balance"));
            var fromBalanceDataProvider = accountFromDataProvider.GetDataProvider().GetDataProvider("Balance");
            var fromBalance = fromBalanceDataProvider.GetAsync(fromBalanceHash).Result;

            var toBalanceHash = new Hash<decimal>(accountTo.CalculateHashWith("Balance"));
            var toBalanceDataProvider = accountToDataProvider.GetDataProvider().GetDataProvider("Balance");
            var toBalance = toBalanceDataProvider.GetAsync(toBalanceHash).Result;

           
            // TODO: calculate with amount and  
            // 

            // TODO: serialize new Balances and uodate
            await accountFromDataProvider.GetDataProvider().GetDataProvider("Balance")
                .SetAsync(fromBalanceHash, fromBalance);
            await accountToDataProvider.GetDataProvider().GetDataProvider("Balance").SetAsync(toBalanceHash, toBalance);
            
        }

        
        /// <summary>
        /// Invoke method in contract
        /// </summary>
        /// <param name="accountFrom"></param>
        /// <param name="accountTo"></param>
        /// <param name="method"></param>
        /// <param name="param"></param>
        /// <returns></returns>
        private async Task InvokeMethod(IAccount accountFrom, IAccount accountTo, string method, object[] param)
        {
            var accountToDaataProvider = _worldState.GetAccountDataProviderByAccount(accountTo);
            var smartConrtract = new SmartContract();
            await smartConrtract.InititalizeAsync(accountToDaataProvider);
            await smartConrtract.InvokeAsync(accountFrom.GetAddress(), method, param);
        }
        
        

        /// <summary>
        /// Create a new account with old contract
        /// </summary>
        /// <param name="accountFrom"></param>
        /// <param name="contractName"></param>
        /// <returns></returns>
        private async Task CreateAccount(IAccount accountFrom, string contractName)
        {
            
            // get the contract regiseter from dataProvider
            var accountZeroDataProvider = _worldState.GetAccountDataProviderByAccount(_accountZero);
            var smartContractRegistration = (SmartContractRegistration) accountZeroDataProvider.GetDataProvider()
                .GetDataProvider("SmartContract")
                .GetAsync(new Hash<SmartContractRegistration>(_accountZero.CalculateHashWith(contractName))).Result;

            // use contract to create new account
            await _accountManager.CreateAccount(accountFrom, smartContractRegistration);
            
        }

        

        /// <summary>
        /// deploy a new smartcontract with tx
        /// and accountTo is created associated with the new contract
        /// </summary>
        /// <param name="accountFrom"></param>
        /// <param name="contractName"></param>
        /// <param name="smartContractCode"></param>
        /// <param name="category"> 1: C# bytes </param>
        private async Task DeploySmartContract(IAccount accountFrom, int category, string contractName, byte[] smartContractCode)
        {
            
            var smartContractRegistration = new SmartContractRegistration
            {
                Name = contractName,
                Bytes = smartContractCode,
                Category = category,
                Hash = new Hash<SmartContractRegistration>(_accountZero.CalculateHashWith(contractName))
            };
            
            // register contracts on accountZero
            var accountZeroDataProvider = _worldState.GetAccountDataProviderByAccount(_accountZero);
            var smartContractZero = new SmartContractZero();
            await smartContractZero.InititalizeAsync(accountZeroDataProvider);
            await smartContractZero.RegisterSmartContract(smartContractRegistration);
            
            // TODO： create new account with contract registered
            await _accountManager.CreateAccount(accountFrom, smartContractRegistration);
        }

       */
        /*
        /// <summary>
        /// Schedule execution of transaction
        /// </summary>
        public void Schedule(List<ITransaction> transactions, IChain chain)
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
            ColorGraph(transactions, chain); 
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
        private void ColorGraph(List<ITransaction> transactions, IChain chain)
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
                    var task = ExecuteAsync(h,chain);
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
            
        }*/
        
    }
}






















