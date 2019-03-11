using AElf.Kernel.SmartContract;

namespace AElf.Sdk.CSharp
{
    public static class SmartContractBridgeContextExtensions
    {
        public static void FireEvent<TEvent>(this ISmartContractBridgeContext context,  TEvent e) where TEvent : Event
        {
            var logEvent = EventParser<TEvent>.ToLogEvent(e, context.Self);
            context.FireLogEvent(logEvent);
        }
    }
}