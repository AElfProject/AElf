using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using Google.Protobuf;
using AElf.Kernel;
using AElf.Types.CSharp;

namespace AElf.Runtime.CSharp
{
    public static class InvokingHelpers
    {
        public static readonly IReadOnlyDictionary<string, Func<MethodInfo, object, object[], Task<IMessage>>>
            AsyncApplyHanders = new Dictionary<string, Func<MethodInfo, object, object[], Task<IMessage>>>()
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

        public static readonly IReadOnlyDictionary<string, Func<MethodInfo, object, object[], IMessage>> ApplyHanders =
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

        public static readonly IReadOnlyDictionary<string, RetVal.Types.RetType> RetTypes =
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
    }
}