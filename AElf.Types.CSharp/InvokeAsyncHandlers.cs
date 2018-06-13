using System;
using System.Threading.Tasks;
using System.Reflection;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Types.CSharp
{
    public static class InvokeAsyncHandlers
    {
        public static Func<MethodInfo, object, object[], Task<Any>> ForVoidReturnType =
            async (m, obj, parameters) =>
            {
                await (Task)m.Invoke(obj, parameters);
                return Any.Pack(new Empty());
            };
        public static Func<MethodInfo, object, object[], Task<Any>> ForBoolReturnType =
            async (m, obj, parameters) =>
            {
                var res = await (Task<bool>)m.Invoke(obj, parameters);
                return res.ToAny();
            };
        public static Func<MethodInfo, object, object[], Task<Any>> ForInt32ReturnType =
            async (m, obj, parameters) =>
            {
                var res = await (Task<int>)m.Invoke(obj, parameters);
                return res.ToAny();
            };
        public static Func<MethodInfo, object, object[], Task<Any>> ForUInt32ReturnType =
            async (m, obj, parameters) =>
            {
                var res = await (Task<uint>)m.Invoke(obj, parameters);
                return res.ToAny();
            };
        public static Func<MethodInfo, object, object[], Task<Any>> ForInt64ReturnType =
            async (m, obj, parameters) =>
            {
                var res = await (Task<long>)m.Invoke(obj, parameters);
                return res.ToAny();
            };
        public static Func<MethodInfo, object, object[], Task<Any>> ForUInt64ReturnType =
            async (m, obj, parameters) =>
            {
                var res = await (Task<ulong>)m.Invoke(obj, parameters);
                return res.ToAny();
            };
        public static Func<MethodInfo, object, object[], Task<Any>> ForStringReturnType =
            async (m, obj, parameters) =>
            {
                var res = await (Task<string>)m.Invoke(obj, parameters);
                return res.ToAny();
            };
        public static Func<MethodInfo, object, object[], Task<Any>> ForBytesReturnType =
            async (m, obj, parameters) =>
            {
                var res = await (Task<byte[]>)m.Invoke(obj, parameters);
                return res.ToAny();
            };
        public static Func<MethodInfo, object, object[], Task<Any>> ForPbMessageReturnType =
            async (m, obj, parameters) =>
            {
                var res = await (Task<IMessage>)m.Invoke(obj, parameters);
                return res.ToAny();
            };
        public static Func<MethodInfo, object, object[], Task<Any>> ForUserTypeReturnType =
            async (m, obj, parameters) =>
            {
                    var res = await (Task<UserType>)m.Invoke(obj, parameters);
                return res.ToAny();
            };
    }
}
