using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AElf.ChainController;
using AElf.Common;
using AElf.Kernel;
using AElf.SmartContract;
using AElf.Common;
using AElf.Kernel.Managers;
using AElf.Kernel.Storages;
using AElf.Types.CSharp;
using Google.Protobuf;

namespace AElf.Execution.Execution
{

    public class SimpleExecutingService : IExecutingService
    {
        private ISmartContractService _smartContractService;
        private ITransactionTraceManager _transactionTraceManager;
        private IChainContextService _chainContextService;
        private IStateManager _stateManager;

        public SimpleExecutingService(ISmartContractService smartContractService,
            ITransactionTraceManager transactionTraceManager, IStateManager stateManager,
            IChainContextService chainContextService)
        {
            _smartContractService = smartContractService;
            _transactionTraceManager = transactionTraceManager;
            _chainContextService = chainContextService;
            _stateManager = stateManager;
        }

        public async Task<List<TransactionTrace>> ExecuteAsync(List<Transaction> transactions, Hash chainId,
            CancellationToken cancellationToken, Hash disambiguationHash = null,
            TransactionType transactionType = TransactionType.ContractTransaction,
            bool skipFee = false)
        {
            var chainContext = await _chainContextService.GetChainContextAsync(chainId);
            var stateCache = new Dictionary<StatePath, StateCache>();
            var traces = new List<TransactionTrace>();
            foreach (var transaction in transactions)
            {
                var trace = await ExecuteOneAsync(0, transaction, chainId, chainContext, stateCache, cancellationToken,
                    skipFee);
                if (!trace.IsSuccessful())
                {
                    trace.SurfaceUpError();
                }

                await trace.SmartCommitChangesAsync(_stateManager);

                if (_transactionTraceManager != null)
                {
                    // Will be null only in tests
                    await _transactionTraceManager.AddTransactionTraceAsync(trace, disambiguationHash);
                }


                traces.Add(trace);

                if (cancellationToken.IsCancellationRequested)
                {
                    break;
                }
            }

//            await _stateDictator.ApplyCachedDataAction(stateCache);
            return traces;
        }

        private async Task<TransactionTrace> ExecuteOneAsync(int depth, Transaction transaction, Hash chainId,
            IChainContext chainContext, Dictionary<StatePath, StateCache> stateCache,
            CancellationToken cancellationToken, bool skipFee = false)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return new TransactionTrace()
                {
                    TransactionId = transaction.GetHash(),
                    StdErr = "Execution Canceled",
                    ExecutionStatus = ExecutionStatus.Canceled
                };
            }

            var trace = new TransactionTrace()
            {
                TransactionId = transaction.GetHash()
            };

            var txCtxt = new TransactionContext()
            {
                PreviousBlockHash = chainContext.BlockHash,
                Transaction = transaction,
                BlockHeight = chainContext.BlockHeight,
                Trace = trace,
                CallDepth = depth
            };

            var executive = await _smartContractService.GetExecutiveAsync(transaction.To, chainId);

            #region Charge Fees

            if (depth == 0 && !skipFee && !UnitTestDetector.IsInUnitTest)
            {
                // Fee is only charged to the main transaction
                var feeAmount = executive.GetFee(transaction.MethodName);
                var chargeFeesTrace = await ChargeTransactionFeesFor(feeAmount, transaction, chainId, chainContext,
                    stateCache, cancellationToken);
                if (chargeFeesTrace.ExecutionStatus == ExecutionStatus.Canceled)
                {
                    return new TransactionTrace()
                    {
                        TransactionId = transaction.GetHash(),
                        StdErr = "Execution Canceled",
                        ExecutionStatus = ExecutionStatus.Canceled
                    };
                }

                if (!chargeFeesTrace.IsSuccessful())
                {
                    return new TransactionTrace()
                    {
                        TransactionId = transaction.GetHash(),
                        ExecutionStatus = ExecutionStatus.InsufficientTransactionFees
                    };
                }

                trace.FeeTransactionTrace = chargeFeesTrace;
            }

            #endregion

            try
            {
                executive.SetDataCache(stateCache);
                await executive.SetTransactionContext(txCtxt).Apply();

                foreach (var kv in txCtxt.Trace.StateChanges)
                {
                    // TODO: Better encapsulation/abstraction for committing to state cache
                    stateCache[kv.StatePath] = new StateCache(kv.StateValue.CurrentValue.ToByteArray());
                }

                foreach (var inlineTx in txCtxt.Trace.InlineTransactions)
                {
                    var inlineTrace = await ExecuteOneAsync(depth + 1, inlineTx, chainId, chainContext, stateCache,
                        cancellationToken, skipFee);
                    trace.InlineTraces.Add(inlineTrace);
                }
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

            return trace;
        }

        private async Task<TransactionTrace> ChargeTransactionFeesFor(ulong feeAmount, Transaction originalTxn,
            Hash chainId,
            IChainContext chainContext, Dictionary<StatePath, StateCache> stateCache,
            CancellationToken cancellationToken)
        {
            var chargeFeesTxn = new Transaction()
            {
                From = originalTxn.From,
                To = ContractHelpers.GetTokenContractAddress(chainId),
                MethodName = "ChargeTransactionFees",
                Params = ByteString.CopyFrom(ParamsPacker.Pack(feeAmount))
            };
            return await ExecuteOneAsync(1, chargeFeesTxn, chainId, chainContext, stateCache, cancellationToken);
        }
    }
}