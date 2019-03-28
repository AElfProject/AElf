using System;
using System.Linq;
using System.Reflection;

namespace AElf.Types.CSharp
{
    public class MethodHandlerForVoid : IMethodHandler
    {
        protected readonly Type[] _parameterTypes;
        protected object _contract;
        protected MethodInfo _methodInfo;

        public MethodHandlerForVoid(MethodInfo methodInfo, object contract)
        {
            _methodInfo = methodInfo;
            _contract = contract;
            _parameterTypes = methodInfo.GetParameters().Select(y => y.ParameterType).ToArray();
        }

        public byte[] Execute(byte[] paramsBytes)
        {
            _methodInfo.Invoke(_contract, GetArguments(paramsBytes));
            return new byte[0];
        }

        public string BytesToString(byte[] bytes)
        {
            return string.Empty;
        }

        public object BytesToReturnType(byte[] bytes)
        {
            return null;
        }

        public object[] GetArguments(byte[] serialized)
        {
            return ParamsPacker.Unpack(serialized, _parameterTypes);
        }
    }
}