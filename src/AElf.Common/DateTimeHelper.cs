using System;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Common
{
    public static class DateTimeHelper
    {
        public static DateTime Now => DateTime.UtcNow;
    }
}
