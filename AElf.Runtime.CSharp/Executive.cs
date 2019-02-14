using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using AElf.Common;
using AElf.Kernel;
using AElf.Kernel.SmartContractExecution.Domain;
using AElf.Runtime.CSharp.Core.ABI;
using Type = System.Type;
using Module = AElf.Kernel.ABI.Module;
using Method = AElf.Kernel.ABI.Method;
using AElf.SmartContract;
using AElf.SmartContract.Contexts;
using AElf.Types.CSharp;

namespace AElf.Runtime.CSharp
{
    public class Executive : IExecutive
    {
        private readonly Module _abi;
//        private readonly Dictionary<string, Method> _methodMap = new Dictionary<string, Method>();
        private MethodsCache _cache;

        private delegate void SetSmartContractContextHandler(ISmartContractContext contractContext);

        private delegate void SetTransactionContextHandler(ITransactionContext transactionContext);

        private SetSmartContractContextHandler _setSmartContractContextHandler;
        private SetTransactionContextHandler _setTransactionContextHandler;
        private ISmartContract _smartContract;
        private ITransactionContext _currentTransactionContext;
        private ISmartContractContext _currentSmartContractContext;
        private IStateManager _stateManager;
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
            _stateManager = stateProviderFactory.CreateStateManager();
            return this;
        }

        public void SetDataCache(Dictionary<StatePath, StateCache> cache)
        {
            _currentSmartContractContext.DataProvider.StateCache = cache;
            _currentSmartContractContext.DataProvider.ClearCache();
        }

        private T GetSetterHandler<T>(Type apiType)
        {
            var methodName = typeof(T).Name.Replace("Handler", "");
            var methodInfo = apiType.GetMethod(methodName,
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
            if (methodInfo == null)
            {
                return (T) (object) null;
            }

            return (T) (object) Delegate.CreateDelegate(typeof(T), methodInfo);
        }

        // ReSharper disable once InconsistentNaming
        internal Executive SetApi(Type ApiType)
        {
            var setSmartContractContext = GetSetterHandler<SetSmartContractContextHandler>(ApiType);
            var setTransactionContext = GetSetterHandler<SetTransactionContextHandler>(ApiType);

            if (setSmartContractContext == null || setTransactionContext == null)
            {
                throw new InvalidOperationException("Input is not a valid Api type");
            }

            _setSmartContractContextHandler = setSmartContractContext;
            _setTransactionContextHandler = setTransactionContext;
            return this;
        }

        internal Executive SetSmartContract(ISmartContract smartContract)
        {
            _smartContract = smartContract;
            _cache = new MethodsCache(_abi, smartContract);
            return this;
        }

        public IExecutive SetSmartContractContext(ISmartContractContext smartContractContext)
        {
            if (_setSmartContractContextHandler == null)
            {
                throw new InvalidOperationException("Api type is not set yet.");
            }

            _setSmartContractContextHandler(smartContractContext);
            _currentSmartContractContext = smartContractContext;
            return this;
        }

        public IExecutive SetTransactionContext(ITransactionContext transactionContext)
        {
            if (_setTransactionContextHandler == null)
            {
                throw new InvalidOperationException("Api type is not set yet.");
            }

            _setTransactionContextHandler(transactionContext);
            _currentTransactionContext = transactionContext;
            return this;
        }

        public async Task Apply()
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

                var tx = _currentTransactionContext.Transaction;

                var handler = _cache.GetHandler(methodName);

                if (handler == null)
                {
                    throw new RuntimeException($"Failed to find handler for {methodName}.");
                }

                try
                {
                    var retVal = await handler(tx.Params.ToByteArray());
                    _currentTransactionContext.Trace.RetVal = retVal;
                    _currentTransactionContext.Trace.ExecutionStatus = ExecutionStatus.ExecutedButNotCommitted;
                }
                catch (TargetInvocationException ex)
                {
                    _currentTransactionContext.Trace.StdErr += ex.InnerException.Message;
                    _currentTransactionContext.Trace.ExecutionStatus = ExecutionStatus.ContractError;
                }
                catch (Exception ex)
                {
                    _currentTransactionContext.Trace.ExecutionStatus = ExecutionStatus.ContractError;
                    _currentTransactionContext.Trace.StdErr += "\n" + ex;
                }


                if (!methodAbi.IsView && _currentTransactionContext.Trace.IsSuccessful() &&
                    _currentTransactionContext.Trace.ExecutionStatus == ExecutionStatus.ExecutedButNotCommitted)
                {
                    var changes = _currentSmartContractContext.DataProvider.GetChanges().Select(kv => new StateChange()
                    {
                        StatePath = kv.Key,
                        StateValue = kv.Value
                    });
                    _currentTransactionContext.Trace.StateChanges.AddRange(changes);
                }
            }
            catch (Exception ex)
            {
                _currentTransactionContext.Trace.ExecutionStatus = ExecutionStatus.SystemError;
                _currentTransactionContext.Trace.StdErr += ex + "\n";
            }

            var e = _currentTransactionContext.Trace.EndTime = DateTime.UtcNow;
            _currentTransactionContext.Trace.Elapsed = (e - s).Ticks;
        }

        public ulong GetFee(string methodName)
        {
            var methodAbi = _cache.GetMethodAbi(methodName);

            return methodAbi.Fee;
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
    }
}