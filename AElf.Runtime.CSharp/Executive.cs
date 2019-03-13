using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using AElf.Common;
using AElf.Kernel;
using AElf.Kernel.Types.SmartContract;
using AElf.Runtime.CSharp.Core.ABI;
using AElf.Types.CSharp;
using Google.Protobuf;
using Module = AElf.Kernel.ABI.Module;
using AElf.Kernel.SmartContract;
using AElf.Kernel.SmartContract.Infrastructure;
using AElf.Kernel.SmartContract.Sdk;
using AElf.Runtime.CSharp.Core;
using AElf.Sdk.CSharp;
using IHostSmartContractBridgeContext = AElf.Kernel.SmartContract.IHostSmartContractBridgeContext;

namespace AElf.Runtime.CSharp
{
    public class Executive : IExecutive
    {
        private readonly Module _abi;
        private MethodsCache _cache;

        private CSharpSmartContractProxy _smartContractProxy;
        private ISmartContract _smartContract;
        private ITransactionContext CurrentTransactionContext => _hostSmartContractBridgeContext.TransactionContext;
        private CachedStateProvider _stateProvider;
        private int _maxCallDepth = 4;

        private IHostSmartContractBridgeContext _hostSmartContractBridgeContext;
        private IServiceContainer<IExecutivePlugin> _executivePlugins;

        public Executive(Module abiModule, IServiceContainer<IExecutivePlugin> executivePlugins)
        {
            _abi = abiModule;
            _executivePlugins = executivePlugins;
        }

        public Hash ContractHash { get; set; }
        public Address ContractAddress { get; set; }

        public IExecutive SetMaxCallDepth(int maxCallDepth)
        {
            _maxCallDepth = maxCallDepth;
            return this;
        }

        public IExecutive SetHostSmartContractBridgeContext(IHostSmartContractBridgeContext smartContractBridgeContext)
        {
            _hostSmartContractBridgeContext = smartContractBridgeContext;
            _smartContractProxy.InternalInitialize(_hostSmartContractBridgeContext);
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


        public IExecutive SetTransactionContext(ITransactionContext transactionContext)
        {
            _hostSmartContractBridgeContext.TransactionContext = transactionContext;
            _stateProvider.TransactionContext = transactionContext;
            return this;
        }

        private void Cleanup()
        {
            _smartContractProxy.Cleanup();
        }

        public async Task Apply()
        {
            await ExecuteMainTransaction();
            //MaybeInsertFeeTransaction();

            foreach (var executivePlugin in _executivePlugins)
            {
                executivePlugin.AfterApply(_smartContract, 
                    _hostSmartContractBridgeContext, ExecuteReadOnlyHandler);
            }
        }

        /*
        public void MaybeInsertFeeTransaction()
        {
            // No insertion of transaction if it's not IFeeChargedContract or it's not top level transaction
            if (!(_smartContract is IFeeChargedContract) || CurrentTransactionContext.CallDepth > 0)
            {
                return;
            }

            CurrentTransactionContext.Trace.InlineTransactions.Add(new Transaction()
            {
                From = CurrentTransactionContext.Transaction.From,
                //TODO: set in constant
                To = _hostSmartContractBridgeContext.GetContractAddressByName(
                    Hash.FromString("AElf.Contracts.Token.TokenContract")),
                MethodName = nameof(ITokenContract.ChargeTransactionFees),
                Params = ByteString.CopyFrom(
                    ParamsPacker.Pack(GetFee(CurrentTransactionContext.Transaction.MethodName)))
            });
        }*/

        public async Task ExecuteMainTransaction()
        {
            if (CurrentTransactionContext.CallDepth > _maxCallDepth)
            {
                CurrentTransactionContext.Trace.ExecutionStatus = ExecutionStatus.ExceededMaxCallDepth;
                CurrentTransactionContext.Trace.StdErr = "\n" + "ExceededMaxCallDepth";
                return;
            }

            var s = CurrentTransactionContext.Trace.StartTime = DateTime.UtcNow;
            var methodName = CurrentTransactionContext.Transaction.MethodName;

            try
            {
                var methodAbi = _cache.GetMethodAbi(methodName);

                var handler = _cache.GetHandler(methodName);
                var tx = CurrentTransactionContext.Transaction;

                if (handler == null)
                {
                    throw new RuntimeException($"Failed to find handler for {methodName}.");
                }

                try
                {
                    var retVal = handler.Execute(tx.Params.ToByteArray());
                    if (retVal != null)
                    {
                        CurrentTransactionContext.Trace.ReturnValue = ByteString.CopyFrom(retVal);
                        CurrentTransactionContext.Trace.ReadableReturnValue = handler.BytesToString(retVal);
                    }

                    CurrentTransactionContext.Trace.ExecutionStatus = ExecutionStatus.Executed;
                }
                catch (TargetInvocationException ex)
                {
                    CurrentTransactionContext.Trace.StdErr += ex.InnerException;
                    CurrentTransactionContext.Trace.ExecutionStatus = ExecutionStatus.ContractError;
                }
                catch (Exception ex)
                {
                    CurrentTransactionContext.Trace.ExecutionStatus = ExecutionStatus.ContractError;
                    CurrentTransactionContext.Trace.StdErr += "\n" + ex;
                }

                if (!methodAbi.IsView && CurrentTransactionContext.Trace.IsSuccessful())
                {
                    CurrentTransactionContext.Trace.StateSet = _smartContractProxy.GetChanges();
                }
                else
                {
                    CurrentTransactionContext.Trace.StateSet = new TransactionExecutingStateSet();
                }
            }
            catch (Exception ex)
            {
                CurrentTransactionContext.Trace.ExecutionStatus = ExecutionStatus.SystemError;
                CurrentTransactionContext.Trace.StdErr += ex + "\n";
            }
            finally
            {
                Cleanup();
            }

            var e = CurrentTransactionContext.Trace.EndTime = DateTime.UtcNow;
            CurrentTransactionContext.Trace.Elapsed = (e - s).Ticks;
        }

        /*
        public ulong GetFee(string methodName)
        {
            var handler = _cache.GetHandler(nameof(IFeeChargedContract.GetMethodFee));
            var retVal = handler.Execute(ParamsPacker.Pack(methodName));
            handler.BytesToReturnType(retVal);
            return (ulong) handler.BytesToReturnType(retVal);
        }*/

        private object ExecuteReadOnlyHandler(string methodName, params object[] objects)
        {
            var handler = _cache.GetHandler(methodName);
            var retVal = handler.Execute(ParamsPacker.Pack(objects));
            handler.BytesToReturnType(retVal);
            return (ulong) handler.BytesToReturnType(retVal);
        }

        public string GetJsonStringOfParameters(string methodName, byte[] paramsBytes)
        {
            // method info 
            var methodInfo = _smartContract.GetType().GetMethod(methodName);
            var parameterNames = methodInfo.GetParameters().Select(y => y.Name);
            var parameterTypes = methodInfo.GetParameters().Select(y => y.ParameterType).ToArray();
            var parameters = ParamsPacker.Unpack(paramsBytes, parameterTypes);

            // get method in abi
            var method = _cache.GetMethodAbi(methodName);

            // deserialize
            var values = method.DeserializeParams(parameters, parameterTypes);
            var formattedParams = parameterNames.Zip(values, Tuple.Create).Select(x => $@"""{x.Item1}"": {x.Item2}");

            return $"{{{string.Join(", ", formattedParams)}}}";
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