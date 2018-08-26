using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AElf.Kernel;
using AElf.SmartContract;

namespace AElf.ChainController
{
    public class SimpleExecutingService : IExecutingService
    {
        private ISmartContractService _smartContractService;
        private IStateDictator _stateDictator;
        private IChainService _chainService;

        public SimpleExecutingService(ISmartContractService smartContractService,
            IStateDictator stateDictator,
            IChainService chainService)
        {
            _smartContractService = smartContractService;
            _stateDictator = stateDictator;
            _chainService = chainService;
        }

        public async Task<List<TransactionTrace>> ExecuteAsync(List<Transaction> transactions, Hash chainId,
            CancellationToken cancellationToken)
        {
            var blockChain = _chainService.GetBlockChain(chainId);
            var traces = new List<TransactionTrace>();
            foreach (var transaction in transactions)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    traces.Add(new TransactionTrace()
                    {
                        TransactionId = transaction.GetHash(),
                        StdErr = "Execution Canceled",
                        ExecutionStatus = ExecutionStatus.Canceled
                    });
                    continue;
                }

                var txCtxt = new TransactionContext()
                {
                    PreviousBlockHash = await blockChain.GetCurrentBlockHashAsync(),
                    Transaction = transaction,
                    BlockHeight = await blockChain.GetCurrentBlockHeightAsync(),
                    Trace = new TransactionTrace()
                    {
                        TransactionId = transaction.GetHash()
                    }
                };

                var executive = await _smartContractService.GetExecutiveAsync(transaction.To, chainId);
                try
                {
                    await executive.SetTransactionContext(txCtxt).Apply(true);
                }
                catch (Exception ex)
                {
                    txCtxt.Trace.ExecutionStatus = ExecutionStatus.ContractError;
                    txCtxt.Trace.StdErr += ex + "\n";
                }
                finally
                {
                    await _smartContractService.PutExecutiveAsync(transaction.To, executive);
                }

                traces.Add(txCtxt.Trace);
            }

            return traces;
        }
    }
}