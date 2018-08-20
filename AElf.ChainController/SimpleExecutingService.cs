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
        private IWorldStateDictator _worldStateDictator;
        private IChainService _chainService;

        public SimpleExecutingService(ISmartContractService smartContractService,
            IWorldStateDictator worldStateDictator,
            IChainService chainService)
        {
            _smartContractService = smartContractService;
            _worldStateDictator = worldStateDictator;
            _chainService = chainService;
        }

        public async Task<List<TransactionTrace>> ExecuteAsync(List<ITransaction> transactions, Hash chainId,
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
                        StdErr = "Execution Cancelled"
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
                    _worldStateDictator.PreBlockHash =
                        await _chainService.GetBlockChain(chainId).GetCurrentBlockHashAsync();
                    await executive.SetTransactionContext(txCtxt).Apply(true);
                }
                catch (Exception ex)
                {
                    // TODO: Improve log
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