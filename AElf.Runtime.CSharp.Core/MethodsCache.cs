using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using AElf.Kernel;
using AElf.Types.CSharp;
using Google.Protobuf;
using Method = AElf.Kernel.ABI.Method;
using Module = AElf.Kernel.ABI.Module;

namespace AElf.Runtime.CSharp
{
    public class MethodsCache
    {
        private readonly Dictionary<string, Method> _methodMap = new Dictionary<string, Method>();
        private readonly object _contractInstance;

        private readonly Dictionary<Method, Func<byte[], Task<RetVal>>> _asyncHandlersCache =
            new Dictionary<Method, Func<byte[], Task<RetVal>>>();

        private readonly Dictionary<Method, Func<byte[], Task<RetVal>>> _handlersCache =
            new Dictionary<Method, Func<byte[], Task<RetVal>>>();

        private readonly Dictionary<string, MethodInfo> _methodInfos = new Dictionary<string, MethodInfo>();

        public MethodsCache(Module abi, object contractInstance)
        {
            foreach (var m in abi.Methods)
            {
                _methodMap.Add(m.Name, m);
            }

            _contractInstance = contractInstance;
            foreach (var m in contractInstance.GetType().GetRuntimeMethods().Where(m => m.IsPublic && !m.IsStatic))
            {;
                _methodInfos[m.Name] = m;
            }
        }

        public Method GetMethodAbi(string methodName)
        {
            if (!_methodMap.TryGetValue(methodName, out var methodAbi))
            {
                throw new InvalidMethodNameException($"Method name {methodName} not found.");
            }

            return methodAbi;
        }
//        if (!_methodMap.TryGetValue(methodName, out var methodAbi))
//        {
//            throw new InvalidMethodNameException($"Method name {methodName} not found.");
//        }

        /// <summary>
        /// Gets a handler from a method abi.
        /// </summary>
        /// <param name="methodName"></param>
        /// <returns></returns>
        public Func<byte[], Task<RetVal>> GetHandler(string methodName)
        {
            var methodAbi = GetMethodAbi(methodName);
            return methodAbi.IsAsync ? GetAsyncHandler(methodAbi) : GetSyncHandler(methodAbi);
        }

        /// <summary>
        /// Gets handler for an async method.
        /// </summary>
        /// <param name="methodAbi"></param>
        /// <returns></returns>
        private Func<byte[], Task<RetVal>> GetAsyncHandler(Method methodAbi)
        {
            if (_asyncHandlersCache.TryGetValue(methodAbi, out var handler))
            {
                return handler;
            }

            var methodInfo = _methodInfos[methodAbi.Name];

            InvokingHelpers.RetTypes.TryGetValue(methodAbi.ReturnType, out var retType);

            if (!InvokingHelpers.AsyncApplyHanders.TryGetValue(methodAbi.ReturnType, out var applyHandler))
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

            var contract = _contractInstance;
            handler = async (paramsBytes) =>
            {
                var parameters = ParamsPacker.Unpack(paramsBytes,
                    methodInfo.GetParameters().Select(y => y.ParameterType).ToArray());
                var msg = await applyHandler(methodInfo, contract, parameters);
                return new RetVal
                {
                    Type = retType,
                    Data = msg.ToByteString()
                };
            };
            _asyncHandlersCache[methodAbi] = handler;
            return handler;
        }

        /// <summary>
        /// Gets handler for non-async method, the output is standardized to an async version. 
        /// </summary>
        /// <param name="methodAbi"></param>
        /// <returns></returns>
        private Func<byte[], Task<RetVal>> GetSyncHandler(Method methodAbi)
        {
            if (_handlersCache.TryGetValue(methodAbi, out var handler))
            {
                return handler;
            }

            InvokingHelpers.RetTypes.TryGetValue(methodAbi.ReturnType, out var retType);

            var methodInfo = _methodInfos[methodAbi.Name];

            if (!InvokingHelpers.ApplyHanders.TryGetValue(methodAbi.ReturnType, out var applyHandler))
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

            var contract = _contractInstance;
            handler = async (paramsBytes) =>
            {
                var parameters = ParamsPacker.Unpack(paramsBytes,
                    methodInfo.GetParameters().Select(y => y.ParameterType).ToArray());
                var msg = applyHandler(methodInfo, contract, parameters);
                return await Task.FromResult(new RetVal()
                {
                    Type = retType,
                    Data = msg.ToByteString()
                });
            };
            _handlersCache[methodAbi] = handler;
            return handler;
        }
    }
}