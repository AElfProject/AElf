﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using AElf.Kernel;
using AElf.Kernel.Managers;
using AElf.Types.CSharp;
using Google.Protobuf;
using Type = System.Type;
using Module = AElf.ABI.CSharp.Module;
using Method = AElf.ABI.CSharp.Method;


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
        
        private readonly Dictionary<string, RetVal.Types.RetType> _retTypes =
            new Dictionary<string, RetVal.Types.RetType>()
            {
                {"void", RetVal.Types.RetType.Void},
                {"bool", RetVal.Types.RetType.Bool},
                {"int", RetVal.Types.RetType.Int32},
                {"uint", RetVal.Types.RetType.Uint32},
                {"long", RetVal.Types.RetType.Int64},
                {"ulong", RetVal.Types.RetType.Uint64},
                {"string", RetVal.Types.RetType.String},
                {"byte[]", RetVal.Types.RetType.Bytes}
            };

        private readonly Dictionary<string, Method> _methodMap = new Dictionary<string, Method>();

        private delegate void SetSmartContractContextHandler(ISmartContractContext contractContext);

        private delegate void SetTransactionContextHandler(ITransactionContext transactionContext);

        private SetSmartContractContextHandler _setSmartContractContextHandler;
        private SetTransactionContextHandler _setTransactionContextHandler;
        private ISmartContract _smartContract;
        private ITransactionContext _currentTransactionContext;
        private ISmartContractContext _currentSmartContractContext;
        private IWorldStateDictator _worldStateDictator;

        public Executive(Module abiModule)
        {
            foreach (var m in abiModule.Methods)
            {
                _methodMap.Add(m.Name, m);
            }
        }

        public IExecutive SetWorldStateManager(IWorldStateDictator worldStateDictator)
        {
            _worldStateDictator = worldStateDictator;
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
            _asyncHandlersCache.Clear();
            _handlersCache.Clear();
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
            var methodName = _currentTransactionContext.Transaction.MethodName;

            try
            {
                if (!_methodMap.TryGetValue(methodName, out var methodAbi))
                {
                    throw new InvalidMethodNameException($"Method name {methodName} not found.");
                }

                var tx = _currentTransactionContext.Transaction;
                if (methodAbi.IsAsync)
                {
                    var handler = GetAsyncHandler(methodAbi);

                    if (handler == null)
                    {
                        throw new RuntimeException($"Failed to find handler for {methodName}.");
                    }

                    try
                    {
                        _currentSmartContractContext.DataProvider.ClearCache();
                        var retVal = await handler(tx.Params.ToByteArray());
                        _currentTransactionContext.Trace.RetVal = retVal;
                    }
                    catch (Exception ex)
                    {
                        _currentTransactionContext.Trace.StdErr += "\n" + ex;
                    }
                }
                else
                {
                    var handler = GetHandler(methodAbi);

                    if (handler == null)
                    {
                        throw new RuntimeException($"Failed to find handler for {methodName}.");
                    }

                    try
                    {
                        _currentSmartContractContext.DataProvider.ClearCache();
                        var retVal = handler(tx.Params.ToByteArray());
                        _currentTransactionContext.Trace.RetVal = retVal;
                    }
                    catch (Exception ex)
                    {
                        _currentTransactionContext.Trace.StdErr += "\n" + ex;
                    }
                }

                if (_currentTransactionContext.Trace.IsSuccessful())
                {
                    _currentTransactionContext.Trace.ValueChanges.AddRange(_currentSmartContractContext.DataProvider
                        .GetValueChanges());
                    if (autoCommit)
                    {
                        await _currentTransactionContext.Trace.CommitChangesAsync(_worldStateDictator,
                            _currentSmartContractContext.ChainId);
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

        #region Cached handlers for this contract

        private readonly Dictionary<Method, Func<byte[], Task<RetVal>>> _asyncHandlersCache =
            new Dictionary<Method, Func<byte[], Task<RetVal>>>();

        private readonly Dictionary<Method, Func<byte[], RetVal>> _handlersCache =
            new Dictionary<Method, Func<byte[], RetVal>>();
        
        /// <summary>
        /// Get async handler from cache or by reflection.
        /// </summary>
        /// <param name="methodAbi">The abi definition of the method.</param>
        /// <returns>An async handler that takes serialized parameters and returns a IMessage.</returns>
        private Func<byte[], Task<RetVal>> GetAsyncHandler(Method methodAbi)
        {
            if (_asyncHandlersCache.TryGetValue(methodAbi, out var handler))
            {
                return handler;
            }

            var methodInfo = _smartContract.GetType().GetMethod(methodAbi.Name);

            _retTypes.TryGetValue(methodAbi.ReturnType, out var retType);
            
            if (!_asyncApplyHanders.TryGetValue(methodAbi.ReturnType, out var applyHandler))
            {
                if (methodInfo.ReturnType.GenericTypeArguments[0].IsPbMessageType())
                {
                    applyHandler = InvokeAsyncHandlers.ForPbMessageReturnType;
                    retType = RetVal.Types.RetType.PbMessage;
                }
                else if (methodInfo.ReturnType.GenericTypeArguments[0].IsUserType())
                {
                    applyHandler = InvokeAsyncHandlers.ForUserTypeReturnType;
                    retType = RetVal.Types.RetType.UserType;
                }
            }

            if (applyHandler == null)
            {
                return null;
            }

            var contract = _smartContract;
            handler = async (paramsBytes) =>
            {
                var parameters = ParamsPacker.Unpack(paramsBytes,
                    methodInfo.GetParameters().Select(y => y.ParameterType).ToArray());
                var msg = await applyHandler(methodInfo, contract, parameters);
                return new RetVal()
                {
                    Type = retType,
                    Data = msg.ToByteString()
                };
            };

            return handler;
        }

        /// <summary>
        /// Get handler from cache or by reflection.
        /// </summary>
        /// <param name="methodAbi">The name of the method.</param>
        /// <returns>A handler that takes serialized parameters and returns a IMessage.</returns>
        private Func<byte[], RetVal> GetHandler(Method methodAbi)
        {
            if (_handlersCache.TryGetValue(methodAbi, out var handler))
            {
                return handler;
            }

            _retTypes.TryGetValue(methodAbi.ReturnType, out var retType);
            
            var methodInfo = _smartContract.GetType().GetMethod(methodAbi.Name);
            if (!_applyHanders.TryGetValue(methodAbi.ReturnType, out var applyHandler))
            {
                if (methodInfo.ReturnType.IsPbMessageType())
                {
                    applyHandler = InvokeHandlers.ForPbMessageReturnType;
                    retType = RetVal.Types.RetType.PbMessage;
                }
                else if (methodInfo.ReturnType.IsUserType())
                {
                    applyHandler = InvokeHandlers.ForUserTypeReturnType;
                    retType = RetVal.Types.RetType.UserType;
                }
            }


            if (applyHandler == null)
            {
                return null;
            }

            var contract = _smartContract;
            handler = (paramsBytes) =>
            {
                var parameters = ParamsPacker.Unpack(paramsBytes,
                    methodInfo.GetParameters().Select(y => y.ParameterType).ToArray());
                var msg = applyHandler(methodInfo, contract, parameters);
                return new RetVal()
                {
                    Type = retType,
                    Data = msg.ToByteString()
                };
            };

            return handler;
        }
        
        #endregion
    }
}