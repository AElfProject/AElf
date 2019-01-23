using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;
using Akka.Actor;
using AElf.Kernel;
using AElf.Configuration;
using AElf.Execution.Scheduling;
using AElf.Common;
using AElf.Execution.Execution;
using Microsoft.Extensions.Options;
using Address = AElf.Common.Address;

namespace AElf.Execution
{
    public class ParallelTransactionExecutingService : IExecutingService
    {
        protected bool TransactionFeeDisabled { get; set; } = false;
        private readonly IGrouper _grouper;
        private readonly IActorEnvironment _actorEnvironment;
        private readonly IExecutingService _simpleExecutingService;
        private readonly ExecutionOptions _executionOptions;

        public ParallelTransactionExecutingService(IActorEnvironment actorEnvironment, IGrouper grouper,
            ServicePack servicePack, IOptionsSnapshot<ExecutionOptions> options)
        {
            _actorEnvironment = actorEnvironment;
            _grouper = grouper;
            _executionOptions = options.Value;
            _simpleExecutingService = new SimpleExecutingService(servicePack.SmartContractService,
                servicePack.TransactionTraceManager, servicePack.StateManager, servicePack.ChainContextService);
        }

        public async Task<List<TransactionTrace>> ExecuteAsync(List<Transaction> transactions, int chainId,
            DateTime currentBlockTime, CancellationToken token, Hash disambiguationHash = null,
            TransactionType transactionType = TransactionType.ContractTransaction,
            bool skipFee = false)
        {
            token.Register(() => _actorEnvironment.Requestor.Tell(JobExecutionCancelMessage.Instance));

            List<List<Transaction>> groups;
            Dictionary<Transaction, Exception> failedTxs = new Dictionary<Transaction, Exception>();
            var results = new List<TransactionTrace>();

            if (transactionType == TransactionType.DposTransaction ||
                transactionType == TransactionType.ContractDeployTransaction)
            {
                results = await _simpleExecutingService.ExecuteAsync(transactions, chainId, currentBlockTime, token,
                    disambiguationHash,
                    transactionType, skipFee || TransactionFeeDisabled);
            }
            else
            {
                //disable parallel module by default because it doesn't finish yet (don't support contract call)
                if (NodeConfig.Instance.ExecutorType == "akka")
                {
                    var groupRes = await _grouper.ProcessWithCoreCount(GroupStrategy.Limited_MaxAddMins,
                        _executionOptions.ConcurrencyLevel, chainId, transactions);
                    groups = groupRes.Item1;
                    failedTxs = groupRes.Item2;
                }
                else
                {
                    groups = new List<List<Transaction>> {transactions};
                }

                var tasks = groups.Select(
                    txs => Task.Run(() => AttemptToSendExecutionRequest(chainId, txs, token, currentBlockTime,
                        disambiguationHash, transactionType, skipFee || TransactionFeeDisabled), token)
                ).ToArray();

                results = (await Task.WhenAll(tasks)).SelectMany(x => x).ToList();
            }

            foreach (var failed in failedTxs)
            {
                var failedTrace = new TransactionTrace
                {
                    StdErr = "Transaction with ID/hash " + failed.Key.GetHash().ToHex() +
                             " failed, detail message: \n" + failed.Value,
                    TransactionId = failed.Key.GetHash()
                };
                results.Add(failedTrace);
                Console.WriteLine(failedTrace.StdErr);
            }

            return results;
        }

        private async Task<List<TransactionTrace>> AttemptToSendExecutionRequest(int chainId,
            List<Transaction> transactions, CancellationToken token, DateTime currentBlockTime, Hash disambiguationHash,
            TransactionType transactionType, bool skipFee)
        {
            while (!token.IsCancellationRequested)
            {
                var tcs = new TaskCompletionSource<List<TransactionTrace>>();
                _actorEnvironment.Requestor.Tell(
                    new LocalExecuteTransactionsMessage(chainId, transactions, tcs, currentBlockTime,
                        disambiguationHash,
                        transactionType, skipFee));
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
    }
}