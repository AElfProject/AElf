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
            _singlExecutingService = new SimpleExecutingService(servicePack.SmartContractService, servicePack.StateDictator, servicePack.ChainContextService);
        }

        public async Task<List<TransactionTrace>> ExecuteAsync(List<Transaction> transactions, Hash chainId,
            CancellationToken token)
        {
            token.Register(() => _actorEnvironment.Requestor.Tell(JobExecutionCancelMessage.Instance));

            List<List<Transaction>> groups;
            Dictionary<Transaction, Exception> failedTxs;

            var dposTxs = transactions.Where(tx => tx.Type == TransactionType.DposTransaction).ToList();
            var normalTxs = transactions.Where(tx => tx.Type != TransactionType.DposTransaction).ToList();

            //disable parallel module by default because it doesn't finish yet (don't support contract call)
            if (ParallelConfig.Instance.IsParallelEnable)
            {
                var groupRes = await _grouper.ProcessWithCoreCount(GroupStrategy.Limited_MaxAddMins,
                    ActorConfig.Instance.ConcurrencyLevel, chainId, normalTxs);
                groups = groupRes.Item1;
                failedTxs = groupRes.Item2;
            }
            else
            {
                groups = new List<List<Transaction>> {normalTxs};
                failedTxs = new Dictionary<Transaction, Exception>();
            }

            var dopsResult = _singlExecutingService.ExecuteAsync(dposTxs, chainId, token);
            var tasks = groups.Select(
                txs => Task.Run(() => AttemptToSendExecutionRequest(chainId, txs, token), token)
            ).ToArray();
            
            var results = dopsResult.Result;
            results.AddRange((await Task.WhenAll(tasks)).SelectMany(x => x).ToList());

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
            List<Transaction> transactions, CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                var tcs = new TaskCompletionSource<List<TransactionTrace>>();
                _actorEnvironment.Requestor.Tell(new LocalExecuteTransactionsMessage(chainId, transactions, tcs));
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
                Transaction = tx,
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