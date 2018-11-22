using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;
using AElf.ChainController;
using Akka.Actor;
using ServiceStack.Text;
using AElf.Kernel;
using AElf.Configuration;
using AElf.Execution.Scheduling;
using AElf.SmartContract;
using AElf.Common;
using AElf.Execution.Execution;
using Address = AElf.Common.Address;

namespace AElf.Execution
{
    public class ParallelTransactionExecutingService : IExecutingService
    {
        private readonly IGrouper _grouper;
        private readonly IActorEnvironment _actorEnvironment;
        private readonly IExecutingService _singlExecutingService;

        // TODO: Move it to config
        public int TimeoutMilliSeconds { get; set; } = int.MaxValue;

        public ParallelTransactionExecutingService(IActorEnvironment actorEnvironment, IGrouper grouper,ServicePack servicePack)
        {
            _actorEnvironment = actorEnvironment;
            _grouper = grouper;
            _singlExecutingService = new SimpleExecutingService(servicePack.SmartContractService, servicePack.TransactionTraceManager, servicePack.StateStore, servicePack.ChainContextService);
        }

        public async Task<List<TransactionTrace>> ExecuteAsync(List<Transaction> transactions, Hash chainId,
            CancellationToken token, Hash disambiguationHash=null, TransactionType transactionType = TransactionType.ContractTransaction)
        {
            token.Register(() => _actorEnvironment.Requestor.Tell(JobExecutionCancelMessage.Instance));

            List<List<Transaction>> groups;
            Dictionary<Transaction, Exception> failedTxs=new Dictionary<Transaction, Exception>();
            var results = new List<TransactionTrace>();

            if (transactionType == TransactionType.DposTransaction || transactionType == TransactionType.ContractDeployTransaction)
            {
                results = await _singlExecutingService.ExecuteAsync(transactions, chainId, token);
                
                if (ActorConfig.Instance.IsCluster)
                {
                    var contractAddresses = new List<Address>();
                    foreach (var tx in transactions)
                    {
                        if (tx.MethodName == "UpdateSmartContract")
                        {
                            contractAddresses.Add(tx.To);
                        }
                    }

                    if (contractAddresses.Count > 0)
                    {
                        _actorEnvironment.Requestor.Tell(new UpdateContractMessage {ContractAddress = contractAddresses});
                    }
                }
            }
            else
            {
                //disable parallel module by default because it doesn't finish yet (don't support contract call)
                if (ParallelConfig.Instance.IsParallelEnable)
                {
                    var groupRes = await _grouper.ProcessWithCoreCount(GroupStrategy.Limited_MaxAddMins,
                        ActorConfig.Instance.ConcurrencyLevel, chainId, transactions);
                    groups = groupRes.Item1;
                    failedTxs = groupRes.Item2;
                }
                else
                {
                    groups = new List<List<Transaction>> {transactions};
                }
                
                var tasks = groups.Select(
                    txs => Task.Run(() => AttemptToSendExecutionRequest(chainId, txs, token, disambiguationHash), token)
                ).ToArray();

                results = (await Task.WhenAll(tasks)).SelectMany(x => x).ToList();
            }

            foreach (var failed in failedTxs)
            {
                var failedTrace = new TransactionTrace
                {
                    StdErr = "Transaction with ID/hash " + failed.Key.GetHash().DumpHex() +
                             " failed, detail message: \n" + failed.Value.Dump(),
                    TransactionId = failed.Key.GetHash()
                };
                results.Add(failedTrace);
                Console.WriteLine(failedTrace.StdErr);
            }
            
            return results;
        }

        private async Task<List<TransactionTrace>> AttemptToSendExecutionRequest(Hash chainId,
            List<Transaction> transactions, CancellationToken token, Hash disambiguationHash)
        {
            while (!token.IsCancellationRequested)
            {
                var tcs = new TaskCompletionSource<List<TransactionTrace>>();
                _actorEnvironment.Requestor.Tell(new LocalExecuteTransactionsMessage(chainId, transactions, tcs, disambiguationHash));
                var traces = await tcs.Task;

                if (traces.Count > 0)
                {
                    return traces;
                }

                Thread.Sleep(1);
            }

            // Cancelled
            return transactions.Select(tx => new TransactionTrace()
            {
                TransactionId = tx.GetHash(),
                ExecutionStatus = ExecutionStatus.Canceled,
                StdErr = "Execution Canceled"
            }).ToList();
        }

        private void CancelExecutions()
        {
            _actorEnvironment.Requestor.Tell(JobExecutionCancelMessage.Instance);
        }
    }
}