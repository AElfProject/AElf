using AElf.Kernel;
using AElf.CSharp.Core;
using AElf.Types;
using Google.Protobuf;

namespace AElf.Sdk.CSharp
{
    public static class EventExtensions
    {
        public static LogEvent ToLogEvent<T>(this T eventData, Address self = null) where T : IEvent<T>
        {
            var logEvent = new LogEvent
            {
                Address = self,
                Name = eventData.Descriptor.Name
            };

            foreach (var indexed in eventData.GetIndexed())
            {
                var byteString = indexed.ToByteString();
                if (byteString.Length == 0)
                {
                    continue;
                }

                logEvent.Indexed.Add(byteString);
            }

            logEvent.NonIndexed = eventData.GetNonIndexed().ToByteString();
            return logEvent;
        }

        public static void MergeFrom<T>(this T eventData, LogEvent log) where T : IEvent<T>
        {
            foreach (var bs in log.Indexed)
            {
                eventData.MergeFrom(bs);
            }

            eventData.MergeFrom(log.NonIndexed);
        }
    }
}