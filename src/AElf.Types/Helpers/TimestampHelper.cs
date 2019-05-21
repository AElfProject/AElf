using System;
using Google.Protobuf.WellKnownTypes;

namespace AElf
{
    public static class TimestampHelper
    {
        public static Timestamp GetUtcNow()
        {
            return DateTime.UtcNow.ToTimestamp();
        }
    }
}