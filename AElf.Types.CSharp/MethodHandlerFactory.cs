using System;
using System.Reflection;

namespace AElf.Types.CSharp
{
    public static class MethodHandlerFactory
    {
        public static IMethodHandler CreateMethodHandler(MethodInfo methodInfo, object contract)
        {
            var returnType = methodInfo.ReturnType;
            if (returnType == typeof(void))
            {
                return new MethodHandlerForVoid(methodInfo, contract);
            }

            if (returnType == typeof(bool))
            {
                return new MethodHandler<bool>(methodInfo, contract);
            }

            if (returnType == typeof(int))
            {
                return new MethodHandler<int>(methodInfo, contract);
            }

            if (returnType == typeof(uint))
            {
                return new MethodHandler<uint>(methodInfo, contract);
            }

            if (returnType == typeof(long))
            {
                return new MethodHandler<long>(methodInfo, contract);
            }

            if (returnType == typeof(ulong))
            {
                return new MethodHandler<ulong>(methodInfo, contract);
            }

            if (returnType == typeof(string))
            {
                return new MethodHandler<string>(methodInfo, contract);
            }

            if (returnType == typeof(byte[]))
            {
                return new MethodHandler<byte[]>(methodInfo, contract);
            }

            if (returnType.IsPbMessageType() || returnType.IsUserType())
            {
                var methodHandlerGeneric = typeof(MethodHandler<>);
                var type = methodHandlerGeneric.MakeGenericType(returnType);
                return (IMethodHandler) Activator.CreateInstance(type, methodInfo, contract);
            }

            throw new NotSupportedException($"Return type {returnType} is not supported.");
        }
    }
}