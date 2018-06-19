using System;
using System.Reflection;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Types.CSharp
{
    public static class InvokeHandlers
    {
        public static Func<MethodInfo, object, object[], IMessage> ForVoidReturnType =
            (m, obj, parameters) =>
            {
                m.Invoke(obj, parameters);
                return new Empty();
            };
        public static Func<MethodInfo, object, object[], IMessage> ForBoolReturnType =
            (m, obj, parameters) =>
            {
                var res = (bool)m.Invoke(obj, parameters);
                return res.ToPbMessage();
            };
        public static Func<MethodInfo, object, object[], IMessage> ForInt32ReturnType =
            (m, obj, parameters) =>
            {
                var res = (int)m.Invoke(obj, parameters);
                return res.ToPbMessage();
            };
        public static Func<MethodInfo, object, object[], IMessage> ForUInt32ReturnType =
            (m, obj, parameters) =>
            {
                var res = (uint)m.Invoke(obj, parameters);
                return res.ToPbMessage();
            };
        public static Func<MethodInfo, object, object[], IMessage> ForInt64ReturnType =
            (m, obj, parameters) =>
            {
                var res = (long)m.Invoke(obj, parameters);
                return res.ToPbMessage();
            };
        public static Func<MethodInfo, object, object[], IMessage> ForUInt64ReturnType =
            (m, obj, parameters) =>
            {
                var res = (ulong)m.Invoke(obj, parameters);
                return res.ToPbMessage();
            };
        public static Func<MethodInfo, object, object[], IMessage> ForStringReturnType =
            (m, obj, parameters) =>
            {
                var res = (string)m.Invoke(obj, parameters);
                return res.ToPbMessage();
            };
        public static Func<MethodInfo, object, object[], IMessage> ForBytesReturnType =
            (m, obj, parameters) =>
            {
                var res = (byte[])m.Invoke(obj, parameters);
                return res.ToPbMessage();
            };
        public static Func<MethodInfo, object, object[], IMessage> ForPbMessageReturnType =
            (m, obj, parameters) =>
            {
                var res = (IMessage)m.Invoke(obj, parameters);
                return res.ToPbMessage();
            };
        public static Func<MethodInfo, object, object[], IMessage> ForUserTypeReturnType =
            (m, obj, parameters) =>
            {
                var res = (UserType)m.Invoke(obj, parameters);
                return res.ToPbMessage();
            };
    }
}
