using System;
using System.Linq;
using System.Reflection;

namespace AElf.Types.CSharp
{
    public class MethodHandler<T> : IMethodHandler
    {
        protected readonly Type[] _parameterTypes;
        protected object _contract;
        protected Func<byte[], T> _decoder;
        protected Func<T, byte[]> _encoder;
        protected MethodInfo _methodInfo;
        protected Func<T, string> _stringConverter;

        public MethodHandler(MethodInfo methodInfo, object contract)
        {
            _methodInfo = methodInfo;
            _contract = contract;
            _parameterTypes = methodInfo.GetParameters().Select(y => y.ParameterType).ToArray();
            _encoder = ReturnTypeHelper.GetEncoder<T>();
            _decoder = ReturnTypeHelper.GetDecoder<T>();
            _stringConverter = ReturnTypeHelper.GetStringConverter<T>();
        }

        public byte[] Execute(byte[] paramsBytes)
        {
            var returnValue = (T) _methodInfo.Invoke(_contract, GetArguments(paramsBytes));
            return _encoder(returnValue);
        }

        public string BytesToString(byte[] bytes)
        {
            return _stringConverter(_decoder(bytes));
        }

        public object BytesToReturnType(byte[] bytes)
        {
            return bytes == null ? default(T) : _decoder(bytes);
        }

        private object[] GetArguments(byte[] serialized)
        {
            return ParamsPacker.Unpack(serialized, _parameterTypes);
        }
    }
}