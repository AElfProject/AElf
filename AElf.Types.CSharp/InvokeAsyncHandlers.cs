using System;
using System.Threading.Tasks;
using System.Reflection;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Types.CSharp
{
    public static class InvokeAsyncHandlers
    {
        public static Func<MethodInfo, object, object[], Task<IMessage>> ForVoidReturnType =
            async (m, obj, parameters) =>
            {
                await (Task)m.Invoke(obj, parameters);
                return new Empty();
            };
        public static Func<MethodInfo, object, object[], Task<IMessage>> ForBoolReturnType =
            async (m, obj, parameters) =>
            {
                var res = await (Task<bool>)m.Invoke(obj, parameters);
                return res.ToPbMessage();
            };
        public static Func<MethodInfo, object, object[], Task<IMessage>> ForInt32ReturnType =
            async (m, obj, parameters) =>
            {
                var res = await (Task<int>)m.Invoke(obj, parameters);
                return res.ToPbMessage();
            };
        public static Func<MethodInfo, object, object[], Task<IMessage>> ForUInt32ReturnType =
            async (m, obj, parameters) =>
            {
                var res = await (Task<uint>)m.Invoke(obj, parameters);
                return res.ToPbMessage();
            };
        public static Func<MethodInfo, object, object[], Task<IMessage>> ForInt64ReturnType =
            async (m, obj, parameters) =>
            {
                var res = await (Task<long>)m.Invoke(obj, parameters);
                return res.ToPbMessage();
            };
        public static Func<MethodInfo, object, object[], Task<IMessage>> ForUInt64ReturnType =
            async (m, obj, parameters) =>
            {
                var res = await (Task<ulong>)m.Invoke(obj, parameters);
                return res.ToPbMessage();
            };
        public static Func<MethodInfo, object, object[], Task<IMessage>> ForStringReturnType =
            async (m, obj, parameters) =>
            {
                var res = await (Task<string>)m.Invoke(obj, parameters);
                return res.ToPbMessage();
            };
        public static Func<MethodInfo, object, object[], Task<IMessage>> ForBytesReturnType =
            async (m, obj, parameters) =>
            {
                var res = await (Task<byte[]>)m.Invoke(obj, parameters);
                return res.ToPbMessage();
            };
        public static Func<MethodInfo, object, object[], Task<IMessage>> ForPbMessageReturnType =
            async (m, obj, parameters) =>
            {
                var t = (Task)m.Invoke(obj, parameters);
                await t.ConfigureAwait(false);
                IMessage res = await (Task<IMessage>)((dynamic)t).Result;
                return res.ToPbMessage();
            };
        public static Func<MethodInfo, object, object[], Task<IMessage>> ForUserTypeReturnType =
            async (m, obj, parameters) =>
            {
                var t = (Task)m.Invoke(obj, parameters);
                await t.ConfigureAwait(false);
                UserType res = await (Task<UserType>)((dynamic)t).Result;
                return res.ToPbMessage();
            };
    }
}
