using System;
using System.Reflection;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Types.CSharp
{
    public static class InvokeHandlers
    {
        public static Func<MethodInfo, object, object[], Any> ForVoidReturnType =
            (m, obj, parameters) =>
            {
                m.Invoke(obj, parameters);
                return Any.Pack(new Empty());
            };
        public static Func<MethodInfo, object, object[], Any> ForBoolReturnType =
            (m, obj, parameters) =>
            {
                var res = (bool)m.Invoke(obj, parameters);
                return res.ToAny();
            };
        public static Func<MethodInfo, object, object[], Any> ForInt32ReturnType =
            (m, obj, parameters) =>
            {
                var res = (int)m.Invoke(obj, parameters);
                return res.ToAny();
            };
        public static Func<MethodInfo, object, object[], Any> ForUInt32ReturnType =
            (m, obj, parameters) =>
            {
                var res = (uint)m.Invoke(obj, parameters);
                return res.ToAny();
            };
        public static Func<MethodInfo, object, object[], Any> ForInt64ReturnType =
            (m, obj, parameters) =>
            {
                var res = (long)m.Invoke(obj, parameters);
                return res.ToAny();
            };
        public static Func<MethodInfo, object, object[], Any> ForUInt64ReturnType =
            (m, obj, parameters) =>
            {
                var res = (ulong)m.Invoke(obj, parameters);
                return res.ToAny();
            };
        public static Func<MethodInfo, object, object[], Any> ForStringReturnType =
            (m, obj, parameters) =>
            {
                var res = (string)m.Invoke(obj, parameters);
                return res.ToAny();
            };
        public static Func<MethodInfo, object, object[], Any> ForBytesReturnType =
            (m, obj, parameters) =>
            {
                var res = (byte[])m.Invoke(obj, parameters);
                return res.ToAny();
            };
        public static Func<MethodInfo, object, object[], Any> ForPbMessageReturnType =
            (m, obj, parameters) =>
            {
                var res = (IMessage)m.Invoke(obj, parameters);
                return res.ToAny();
            };
        public static Func<MethodInfo, object, object[], Any> ForUserTypeReturnType =
            (m, obj, parameters) =>
            {
                var res = (UserType)m.Invoke(obj, parameters);
                return res.ToAny();
            };
    }
}
