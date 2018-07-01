using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;
using Akka.Actor;
using AElf.Kernel.Concurrency.Execution.Messages;
using AElf.Kernel.Concurrency.Scheduling;
using ServiceStack.Text;

namespace AElf.Kernel.Concurrency
{
    public class ParallelTransactionExecutingService : IParallelTransactionExecutingService
    {
        private readonly IGrouper _grouper;
        private readonly IActorRef _requestor;

        // TODO: Move it to config
        public int TimeoutMilliSeconds { get; set; } = int.MaxValue;

        public ParallelTransactionExecutingService(IActorRef requestor, IGrouper grouper)
        {
            _requestor = requestor;
            _grouper = grouper;
        }

        public async Task<List<TransactionTrace>> ExecuteAsync(List<ITransaction> transactions, Hash chainId)
        {
            using (var cts = new CancellationTokenSource())
            using (new Timer(
                CancelExecutions, cts, TimeSpan.FromMilliseconds(TimeoutMilliSeconds),
                TimeSpan.FromMilliseconds(-1)
            ))
            {
                cts.CancelAfter(TimeoutMilliSeconds);

#if PARALLEL
                var grouped = _grouper.Process(chainId, transactions, out var failedTxs);
#else
                var grouped = new List<List<ITransaction>>() {transactions};
#endif
                var tasks = grouped.Select(
                    txs => Task.Run(() => AttemptToSendExecutionRequest(chainId, txs, cts.Token), cts.Token)
                ).ToArray();
                var results = (await Task.WhenAll(tasks)).SelectMany(x => x).ToList();

                foreach (var failed in failedTxs)
                {
                    var failedTrace = new TransactionTrace
                    {
                        StdErr = "Transaction with ID/hash " + failed.Key.GetHash().Value.ToBase64() +
                                 " failed, detail message: \n" + failed.Value.Dump(),
                        TransactionId = failed.Key.GetHash()
                    };
                    results.Add(failedTrace);
                }

                return results;
            }
        }

        private async Task<List<TransactionTrace>> AttemptToSendExecutionRequest(Hash chainId,
            List<ITransaction> transactions, CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                var tcs = new TaskCompletionSource<List<TransactionTrace>>();
                _requestor.Tell(new LocalExecuteTransactionsMessage(chainId, transactions, tcs));
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
                StdErr = "Execution Cancelled"
            }).ToList();
        }

        private void CancelExecutions(object stateInfo)
        {
            _requestor.Tell(JobExecutionCancelMessage.Instance);
        }
    }
}