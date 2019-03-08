using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using AElf.Common;
using AElf.Kernel;
using AElf.Kernel.Types;
using AElf.Kernel.Types.SmartContract;
using AElf.Runtime.CSharp.Core.ABI;
using AElf.Types.CSharp;
using Google.Protobuf;
using Module = AElf.Kernel.ABI.Module;
using AElf.Kernel.SmartContract;
using AElf.Kernel.SmartContract.Contexts;
using AElf.Kernel.SmartContract.Infrastructure;
using AElf.Runtime.CSharp.Core;

namespace AElf.Runtime.CSharp
{
    public class Executive : IExecutive
    {
        private readonly Module _abi;
        private MethodsCache _cache;

        private CSharpSmartContractProxy _smartContractProxy;
        private ISmartContract _smartContract;
        private ITransactionContext _currentTransactionContext;
        private ISmartContractContext _currentSmartContractContext;
        private CachedStateProvider _stateProvider;
        private int _maxCallDepth = 4;

        public Executive(Module abiModule)
        {
            _abi = abiModule;
        }

        public Hash ContractHash { get; set; }

        public IExecutive SetMaxCallDepth(int maxCallDepth)
        {
            _maxCallDepth = maxCallDepth;
            return this;
        }

        public IExecutive SetStateProviderFactory(IStateProviderFactory stateProviderFactory)
        {
            _stateProvider = new CachedStateProvider(stateProviderFactory.CreateStateProvider());
            _smartContractProxy.SetStateProvider(_stateProvider);
            return this;
        }

        public void SetDataCache(IStateCache cache)
        {
            _stateProvider.Cache = cache ?? new NullStateCache();
        }

        public Executive SetSmartContract(ISmartContract smartContract)
        {
            _smartContract = smartContract;
            _smartContractProxy = new CSharpSmartContractProxy(smartContract);
            _cache = new MethodsCache(_abi, smartContract);
            return this;
        }

        public IExecutive SetSmartContractContext(ISmartContractContext smartContractContext)
        {
            _smartContractProxy.SetSmartContractContext(smartContractContext);
            _currentSmartContractContext = smartContractContext;
            return this;
        }

        public IExecutive SetTransactionContext(ITransactionContext transactionContext)
        {
            _smartContractProxy.SetTransactionContext(transactionContext);
            _currentTransactionContext = transactionContext;
            return this;
        }

        private void Cleanup()
        {
            _smartContractProxy.Cleanup();
        }

        public async Task Apply()
        {
            await ExecuteMainTransaction();
            MaybeInsertFeeTransaction();
        }

        public void MaybeInsertFeeTransaction()
        {
            // No insertion of transaction if it's not IFeeChargedContract or it's not top level transaction
            if (!(_smartContract is IFeeChargedContract) || _currentTransactionContext.CallDepth > 0)
            {
                return;
            }

            _currentTransactionContext.Trace.InlineTransactions.Add(new Transaction()
            {
                From = _currentTransactionContext.Transaction.From,
                //TODO: set in constant
                To = _currentSmartContractContext.GetAddressByContractName(
                    Hash.FromString("AElf.Contracts.Token.TokenContract")),
                MethodName = nameof(ITokenContract.ChargeTransactionFees),
                Params = ByteString.CopyFrom(
                    ParamsPacker.Pack(GetFee(_currentTransactionContext.Transaction.MethodName)))
            });
        }

        public async Task ExecuteMainTransaction()
        {
            if (_currentTransactionContext.CallDepth > _maxCallDepth)
            {
                _currentTransactionContext.Trace.ExecutionStatus = ExecutionStatus.ExceededMaxCallDepth;
                _currentTransactionContext.Trace.StdErr = "\n" + "ExceededMaxCallDepth";
                return;
            }

            var s = _currentTransactionContext.Trace.StartTime = DateTime.UtcNow;
            var methodName = _currentTransactionContext.Transaction.MethodName;

            try
            {
                var methodAbi = _cache.GetMethodAbi(methodName);

                var handler = _cache.GetHandler(methodName);
                var tx = _currentTransactionContext.Transaction;

                if (handler == null)
                {
                    throw new RuntimeException($"Failed to find handler for {methodName}.");
                }

                try
                {
                    var retVal = handler.Execute(tx.Params.ToByteArray());
                    _currentTransactionContext.Trace.RetVal = new RetVal()
                    {
                        Data = retVal == null ? null : ByteString.CopyFrom(retVal)
                    };
                    _currentTransactionContext.Trace.ReadableReturnValue = handler.BytesToString(retVal);
                    _currentTransactionContext.Trace.ExecutionStatus = ExecutionStatus.ExecutedAndCommitted;
                }
                catch (TargetInvocationException ex)
                {
                    _currentTransactionContext.Trace.StdErr += ex.InnerException;
                    _currentTransactionContext.Trace.ExecutionStatus = ExecutionStatus.ContractError;
                }
                catch (Exception ex)
                {
                    _currentTransactionContext.Trace.ExecutionStatus = ExecutionStatus.ContractError;
                    _currentTransactionContext.Trace.StdErr += "\n" + ex;
                }

                if (!methodAbi.IsView && _currentTransactionContext.Trace.IsSuccessful() &&
                    _currentTransactionContext.Trace.ExecutionStatus == ExecutionStatus.ExecutedAndCommitted)
                {
                    _currentTransactionContext.Trace.StateSet = _smartContractProxy.GetChanges();
//                    var changes = _smartContractProxy.GetChanges().Select(kv => new StateChange()
//                    {
//                        StatePath = kv.Key,
//                        StateValue = kv.Value
//                    });
//                    _currentTransactionContext.Trace.StateChanges.AddRange(changes);
                }
                else
                {
                    _currentTransactionContext.Trace.StateSet = new TransactionExecutingStateSet();
                }
            }
            catch (Exception ex)
            {
                _currentTransactionContext.Trace.ExecutionStatus = ExecutionStatus.SystemError;
                _currentTransactionContext.Trace.StdErr += ex + "\n";
            }
            finally
            {
                Cleanup();
            }

            var e = _currentTransactionContext.Trace.EndTime = DateTime.UtcNow;
            _currentTransactionContext.Trace.Elapsed = (e - s).Ticks;
        }

        public ulong GetFee(string methodName)
        {
            var handler = _cache.GetHandler(nameof(IFeeChargedContract.GetMethodFee));
            var retVal = handler.Execute(ParamsPacker.Pack(methodName));
            handler.BytesToReturnType(retVal);
            return (ulong)handler.BytesToReturnType(retVal);
        }

        public string GetJsonStringOfParameters(string methodName, byte[] paramsBytes)
        {
            // method info 
            var methodInfo = _smartContract.GetType().GetMethod(methodName);
            var parameters = ParamsPacker.Unpack(paramsBytes,
                methodInfo.GetParameters().Select(y => y.ParameterType).ToArray());
            // get method in abi
            var method = _cache.GetMethodAbi(methodName);

            // deserialize
            return string.Join(",", method.DeserializeParams(parameters));
        }

        public object GetReturnValue(string methodName, byte[] bytes)
        {
            var handler = _cache.GetHandler(methodName);

            if (handler == null)
            {
                throw new RuntimeException($"Failed to find handler for {methodName}.");
            }

            return handler.BytesToReturnType(bytes);
        }
    }
}