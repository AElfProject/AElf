﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;
using Akka.Actor;
using AElf.Kernel.Concurrency.Execution.Messages;
using AElf.Kernel.Concurrency.Scheduling;

namespace AElf.Kernel.Concurrency
{
    public class ParallelTransactionExecutingService : IParallelTransactionExecutingService
    {
        private readonly IGrouper _grouper;
        private readonly IActorRef _requestor;

        public ParallelTransactionExecutingService(IActorRef requestor, IGrouper grouper)
        {
            _requestor = requestor;
            _grouper = grouper;
        }

        public async Task<List<TransactionTrace>> ExecuteAsync(List<ITransaction> transactions, Hash chainId)
        {
            // TODO: Move it to config
            int timeoutMilliSeconds = 200000;

            var cts = new CancellationTokenSource();

            cts.CancelAfter(timeoutMilliSeconds);

            using (new Timer(
                CancelExecutions, cts, TimeSpan.FromMilliseconds(timeoutMilliSeconds),
                TimeSpan.FromMilliseconds(-1)
            ))
            {
                //TODO: the core count should in the configure file
                var tasks = _grouper.ProcessWithCoreCount(8, chainId, transactions).Select(
                    txs => Task.Run(() => AttemptToSendExecutionRequest(chainId, txs, cts.Token), cts.Token)
                ).ToArray();

                var results = await Task.WhenAll(tasks);

                return results.SelectMany(x => x).ToList();
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