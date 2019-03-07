using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using AElf.Types.CSharp;
using Method = AElf.Kernel.ABI.Method;
using Module = AElf.Kernel.ABI.Module;

namespace AElf.Runtime.CSharp.Core
{
    public class MethodsCache
    {
        private readonly object _contractInstance;
        private readonly Dictionary<string, Method> _abiMap = new Dictionary<string, Method>();
        private readonly Dictionary<string, IMethodHandler> _handlerMap = new Dictionary<string, IMethodHandler>();

        public MethodsCache(Module abi, object contractInstance)
        {
            _contractInstance = contractInstance;
            foreach (var m in abi.Methods)
            {
                _abiMap.Add(m.Name, m);
            }

            foreach (var m in contractInstance.GetType().GetRuntimeMethods().Where(m => m.IsPublic && !m.IsStatic))
            {
                if (!m.ReturnType.IsAllowedType())
                {
                    continue;
                }
                _handlerMap[m.Name] = MethodHandlerFactory.CreateMethodHandler(m, contractInstance);
            }
        }

        public Method GetMethodAbi(string methodName)
        {
            if (!_abiMap.TryGetValue(methodName, out var methodAbi))
            {
                throw new InvalidMethodNameException($"Method name {methodName} not found.");
            }

            return methodAbi;
        }

        /// <summary>
        /// Gets a handler from a method abi.
        /// </summary>
        /// <param name="methodName"></param>
        /// <returns></returns>
        public IMethodHandler GetHandler(string methodName)
        {
            if (!_handlerMap.TryGetValue(methodName, out var handler))
            {
                throw new InvalidMethodNameException($"Method name {methodName} not found.");
            }

            return handler;
        }
    }
}