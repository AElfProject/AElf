using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using AElf.Kernel;
using AElf.Kernel.Managers;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Type = System.Type;
using Module = AElf.ABI.CSharp.Module;
using Method = AElf.ABI.CSharp.Method;
using AElf.Sdk.CSharp.Types;
using AElf.Types.CSharp;

//using Any = Google.Protobuf.WellKnownTypes.Any;
//using StringValue = Google.Protobuf.WellKnownTypes.StringValue;
//using BoolValue = Google.Protobuf.WellKnownTypes.BoolValue;

namespace AElf.Runtime.CSharp
{
    public class Executive : IExecutive
    {
        private readonly Dictionary<string, Func<MethodInfo, object, object[], Task<IMessage>>> _asyncApplyHanders =
            new Dictionary<string, Func<MethodInfo, object, object[], Task<IMessage>>>()
            {
                {"void", InvokeAsyncHandlers.ForVoidReturnType},
                {"bool", InvokeAsyncHandlers.ForBoolReturnType},
                {"int", InvokeAsyncHandlers.ForInt32ReturnType},
                {"uint", InvokeAsyncHandlers.ForUInt32ReturnType},
                {"long", InvokeAsyncHandlers.ForInt64ReturnType},
                {"ulong", InvokeAsyncHandlers.ForUInt64ReturnType},
                {"string", InvokeAsyncHandlers.ForStringReturnType},
                {"byte[]", InvokeAsyncHandlers.ForBytesReturnType}
            };

        private readonly Dictionary<string, Func<MethodInfo, object, object[], IMessage>> _applyHanders =
            new Dictionary<string, Func<MethodInfo, object, object[], IMessage>>()
            {
                {"void", InvokeHandlers.ForVoidReturnType},
                {"bool", InvokeHandlers.ForBoolReturnType},
                {"int", InvokeHandlers.ForInt32ReturnType},
                {"uint", InvokeHandlers.ForUInt32ReturnType},
                {"long", InvokeHandlers.ForInt64ReturnType},
                {"ulong", InvokeHandlers.ForUInt64ReturnType},
                {"string", InvokeHandlers.ForStringReturnType},
                {"byte[]", InvokeHandlers.ForBytesReturnType}
            };

        private readonly Dictionary<string, Method> _methodMap = new Dictionary<string, Method>();

        private delegate void SetSmartContractContextHandler(ISmartContractContext contractContext);

        private delegate void SetTransactionContextHandler(ITransactionContext transactionContext);

        private SetSmartContractContextHandler _setSmartContractContextHandler;
        private SetTransactionContextHandler _setTransactionContextHandler;
        private ISmartContract _smartContract;
        private ITransactionContext _currentTransactionContext;
        private ISmartContractContext _currentSmartContractContext;
        private IWorldStateManager _worldStateManager;

        public Executive(Module abiModule)
        {
            foreach (var m in abiModule.Methods)
            {
                _methodMap.Add(m.Name, m);
            }
        }

        public IExecutive SetWorldStateManager(IWorldStateManager worldStateManager)
        {
            _worldStateManager = worldStateManager;
            return this;
        }

        public Executive SetApi(Type ApiType)
        {
            var scc = ApiType.GetMethod("SetSmartContractContext", BindingFlags.Public | BindingFlags.Static);
            var stc = ApiType.GetMethod("SetTransactionContext", BindingFlags.Public | BindingFlags.Static);
            var scch = Delegate.CreateDelegate(typeof(SetSmartContractContextHandler), scc);
            var stch = Delegate.CreateDelegate(typeof(SetTransactionContextHandler), stc);

            if (scch == null || stch == null)
            {
                throw new InvalidOperationException("Input is not a valid Api type");
            }

            _setSmartContractContextHandler = (SetSmartContractContextHandler) scch;
            _setTransactionContextHandler = (SetTransactionContextHandler) stch;

            return this;
        }

        public Executive SetSmartContract(ISmartContract smartContract)
        {
            _smartContract = smartContract;
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

        public async Task Apply(bool autoCommit)
        {
            var s = _currentTransactionContext.Trace.StartTime = DateTime.UtcNow;
            try
            {
                var methodName = _currentTransactionContext.Transaction.MethodName;
                if (_methodMap.TryGetValue(methodName, out var methodAbi))
                {
                    var methodInfo = _smartContract.GetType().GetMethod(methodName);
                    var tx = _currentTransactionContext.Transaction;
                    //var parameters = Parameters.Parser.ParseFrom(tx.Params).Params.Select(p => p.Value()).ToArray();
                    var parameters = ParamsPacker.Unpack(tx.Params.ToByteArray(),
                        methodInfo.GetParameters().Select(y => y.ParameterType).ToArray());
                    if (methodAbi.IsAsync)
                    {
                        if (!_asyncApplyHanders.TryGetValue(methodAbi.ReturnType, out var handler))
                        {
                            if (methodInfo.ReturnType.GenericTypeArguments[0].IsPbMessageType())
                            {
                                handler = InvokeAsyncHandlers.ForPbMessageReturnType;
                            }
                            else if (methodInfo.ReturnType.GenericTypeArguments[0].IsUserType())
                            {
                                handler = InvokeAsyncHandlers.ForUserTypeReturnType;
                            }
                        }

                        if (handler != null)
                        {
                            try
                            {
                                _currentSmartContractContext.DataProvider.ClearCache();
                                var retMsg = await handler(methodInfo, _smartContract, parameters);
                                _currentTransactionContext.Trace.RetVal = ByteString.CopyFrom(retMsg.ToByteArray());
                            }
                            catch (Exception ex)
                            {
                                _currentTransactionContext.Trace.StdErr += "\n" + ex;
                            }
                        }
                        else
                        {
                            throw new Exception($"Method has an invalid return type {methodInfo.ReturnType}.");
                        }
                    }
                    else
                    {
                        if (_applyHanders.TryGetValue(methodAbi.ReturnType, out var handler))
                        {
                            if (methodInfo.ReturnType.IsPbMessageType())
                            {
                                handler = InvokeHandlers.ForPbMessageReturnType;
                            }
                            else if (methodInfo.ReturnType.IsUserType())
                            {
                                handler = InvokeHandlers.ForUserTypeReturnType;
                            }
                        }

                        if (handler != null)
                        {
                            try
                            {
                                _currentSmartContractContext.DataProvider.ClearCache();
                                var retMsg = handler(methodInfo, _smartContract, parameters);
                                _currentTransactionContext.Trace.RetVal = ByteString.CopyFrom(retMsg.ToByteArray());
                            }
                            catch (Exception ex)
                            {
                                _currentTransactionContext.Trace.StdErr += "\n" + ex;
                            }
                        }
                        else
                        {
                            throw new Exception($"Method has an invalid return type {methodInfo.ReturnType}.");
                        }
                    }

                    if (_currentTransactionContext.Trace.IsSuccessful())
                    {
                        _currentTransactionContext.Trace.ValueChanges.AddRange(_currentSmartContractContext.DataProvider
                            .GetValueChanges());
                        if (autoCommit)
                        {
                            await _currentTransactionContext.Trace.CommitChangesAsync(_worldStateManager,
                                _currentSmartContractContext.ChainId);                            
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _currentTransactionContext.Trace.StdErr += ex + "\n";
            }

            var e = _currentTransactionContext.Trace.EndTime = DateTime.UtcNow;
            _currentTransactionContext.Trace.Elapsed = (e - s).Ticks;
        }
    }
}