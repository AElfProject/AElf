using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using AElf.Common;
using AElf.Kernel;
using AElf.Kernel.Managers;
using AElf.Types.CSharp;
using Google.Protobuf;
using Type = System.Type;
using Module = AElf.ABI.CSharp.Module;
using Method = AElf.ABI.CSharp.Method;
using AElf.SmartContract;

namespace AElf.Runtime.CSharp
{
    public class Executive2 : IExecutive
    {
        private readonly Dictionary<string, Method> _methodMap = new Dictionary<string, Method>();
        private MethodsCache _cache;

        private CSharpSmartContractProxy _smartContractProxy;
        private ISmartContract _smartContract;
        private ITransactionContext _currentTransactionContext;
        private ISmartContractContext _currentSmartContractContext;
        private CachedStateManager _stateManager;
        private int _maxCallDepth = 4;

        public Executive2(Module abiModule)
        {
            foreach (var m in abiModule.Methods)
            {
                _methodMap.Add(m.Name, m);
            }
        }

        public Hash ContractHash { get; set; }

        public IExecutive SetMaxCallDepth(int maxCallDepth)
        {
            _maxCallDepth = maxCallDepth;
            return this;
        }

        public IExecutive SetStateManager(IStateManager stateManager)
        {
            _stateManager = new CachedStateManager(stateManager);
            _smartContractProxy.SetStateManager(_stateManager);
            return this;
        }

        public void SetDataCache(Dictionary<StatePath, StateCache> cache)
        {
            _stateManager.Cache = cache;
        }

        public Executive2 SetSmartContract(ISmartContract smartContract)
        {
            _smartContract = smartContract;
            _smartContractProxy = new CSharpSmartContractProxy(smartContract);
            _cache = new MethodsCache(smartContract);
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
                if (!_methodMap.TryGetValue(methodName, out var methodAbi))
                {
                    throw new InvalidMethodNameException($"Method name {methodName} not found.");
                }

                var tx = _currentTransactionContext.Transaction;
                var handler = _cache.GetHandler(methodAbi);

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
                    var changes = _smartContractProxy.GetChanges().Select(kv => new StateChange()
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
            finally
            {
                Cleanup();
            }

            var e = _currentTransactionContext.Trace.EndTime = DateTime.UtcNow;
            _currentTransactionContext.Trace.Elapsed = (e - s).Ticks;
        }

        public ulong GetFee(string methodName)
        {
            if (!_methodMap.TryGetValue(methodName, out var methodAbi))
            {
                throw new InvalidMethodNameException($"Method name {methodName} not found.");
            }

            return methodAbi.Fee;
        }
    }
}