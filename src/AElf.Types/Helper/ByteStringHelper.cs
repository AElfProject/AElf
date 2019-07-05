using System;
using Google.Protobuf;

namespace AElf
{
    public static class ByteStringHelper
    {
        public static int Compare(ByteString xValue, ByteString yValue)
        {
            for (var i = 0; i < Math.Min(xValue.Length, yValue.Length); i++)
            {
                if (xValue[i] > yValue[i])
                {
                    return 1;
                }

                if (xValue[i] < yValue[i])
                {
                    return -1;
                }
            }

            return 0;
        }
    }
}