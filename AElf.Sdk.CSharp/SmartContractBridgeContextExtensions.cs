using AElf.Common;
using AElf.Kernel;
using AElf.Kernel.SmartContract.Sdk;
using Google.Protobuf;

namespace AElf.Sdk.CSharp
{
    public static class SmartContractBridgeContextExtensions
    {
        public static void FireEvent<TEvent>(this ISmartContractBridgeContext context, TEvent e) where TEvent : Event
        {
            var logEvent = EventParser<TEvent>.ToLogEvent(e, context.Self);
            context.FireLogEvent(logEvent);
        }

        //TODO: Add SmartContractBridgeContextExtensions test case [Case]

        public static T Call<T>(this ISmartContractBridgeContext context, IStateCache stateCache, Address address,
            string methodName, IMessage message)
        {
            return context.Call<T>(stateCache, address, methodName, ConvertToByteString(message));
        }

        public static void SendInline(this ISmartContractBridgeContext context, Address toAddress, string methodName,
            IMessage message)
        {
            context.SendInline(toAddress, methodName, ConvertToByteString(message));
        }

        public static void SendVirtualInline(this ISmartContractBridgeContext context, Hash fromVirtualAddress,
            Address toAddress, string methodName, IMessage message)
        {
            context.SendVirtualInline(fromVirtualAddress, toAddress, methodName,
                ConvertToByteString(message));
        }

        public static ByteString ConvertToByteString(IMessage message)
        {
            return message?.ToByteString() ?? ByteString.Empty;
            //return ByteString.CopyFrom(ParamsPacker.Pack(message));
        }
    }
}