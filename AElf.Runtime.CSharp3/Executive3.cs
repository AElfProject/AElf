using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
    public class Executive3 : IExecutive
    {
        private readonly Type _contractType;
        private readonly object _contractInstance;
        private readonly ReadOnlyDictionary<string, IServerCallHandler> _callHandlers;

        private CSharpSmartContractProxy _smartContractProxy;
        private ITransactionContext CurrentTransactionContext => _hostSmartContractBridgeContext.TransactionContext;
        private CachedStateProvider _stateProvider;
        private int _maxCallDepth = 4;

        private IHostSmartContractBridgeContext _hostSmartContractBridgeContext;

        private Type FindContractType(Assembly assembly)
        {
            var types = assembly.GetTypes();
            return types.SingleOrDefault(t => typeof(ISmartContract).IsAssignableFrom(t) && !t.IsNested);
        }
        private Type FindContractBaseType(Assembly assembly)
        {
            var types = assembly.GetTypes();
            return types.SingleOrDefault(t => typeof(ISmartContract).IsAssignableFrom(t) && t.IsNested);
        }
        private Type FindContractContainer(Assembly assembly)
        {
            var contractBase = FindContractBaseType(assembly);
            return contractBase.DeclaringType;
        }

        private ReadOnlyDictionary<string, IServerCallHandler> GetHandlers(Assembly assembly)
        {
            var methodInfo = FindContractContainer(assembly).GetMethod("BindService",
                new[] {FindContractBaseType(assembly)});
            var ssd = methodInfo.Invoke(null, new[] {_contractInstance}) as ServerServiceDefinition;
            return ssd.GetCallHandlers();
        }
        public Executive3(Assembly assembly)
        {
            _contractType = FindContractType(assembly);
            _contractInstance = Activator.CreateInstance(_contractType);
            _smartContractProxy = new CSharpSmartContractProxy(_contractInstance);
            _callHandlers = GetHandlers(assembly);
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
            MaybeInsertFeeTransaction();
        }

        public void MaybeInsertFeeTransaction()
        {
            // No insertion of transaction if it's not IFeeChargedContract or it's not top level transaction
            if (!(_contractInstance is IFeeChargedContract) || CurrentTransactionContext.CallDepth > 0)
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
        }

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
                if (!_callHandlers.TryGetValue(methodName, out var handler))
                {
                    throw new RuntimeException($"Failed to find handler for {methodName}. We have {_callHandlers.Count} handlers.");
                }
                
                try
                {
                    var tx = CurrentTransactionContext.Transaction;
                    var retVal = handler.Execute(tx.Params.ToByteArray());
                    if (retVal != null)
                    {
                        CurrentTransactionContext.Trace.ReturnValue = ByteString.CopyFrom(retVal);
                        CurrentTransactionContext.Trace.ReadableReturnValue = handler.ReturnBytesToString(retVal);
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

                if (!handler.IsView() && CurrentTransactionContext.Trace.IsSuccessful())
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

        public ulong GetFee(string methodName)
        {
            if (!_callHandlers.TryGetValue(nameof(IFeeChargedContract.GetMethodFee), out var handler))
            {
                return 0;
            }
            var retVal = handler.Execute(ParamsPacker.Pack(methodName));
            return (ulong) handler.ReturnBytesToObject(retVal);
        }

        public string GetJsonStringOfParameters(string methodName, byte[] paramsBytes)
        {
            if (!_callHandlers.TryGetValue(methodName, out var handler))
            {
                return "";
            }

            return handler.InputBytesToString(paramsBytes);
        }

        public object GetReturnValue(string methodName, byte[] bytes)
        {
            if (!_callHandlers.TryGetValue(methodName, out var handler))
            {
                return null;
            }

            return handler.ReturnBytesToObject(bytes);
        }
    }
}