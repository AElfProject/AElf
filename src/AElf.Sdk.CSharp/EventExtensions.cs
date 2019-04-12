using AElf.Common;
using AElf.Kernel;
using AElf.Types.CSharp;
using Google.Protobuf;

namespace AElf.Sdk.CSharp
{
    public static class EventExtensions
    {
        public static LogEvent ToLogEvent<T>(this T eventData, Address self = null) where T : IEvent<T>
        {
            var le = new LogEvent()
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

                le.Indexed.Add(byteString);
            }

            le.NonIndexed = eventData.GetNonIndexed().ToByteString();
            return le;
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