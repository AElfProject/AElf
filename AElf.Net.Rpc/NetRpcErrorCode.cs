using System.Collections.Generic;

namespace AElf.Net.Rpc
{
    public static class Error
    {
        public const long InvalidNetworkAddress = 30001;

        public static readonly Dictionary<long, string> Message = new Dictionary<long, string>
        {
            {InvalidNetworkAddress, "Invalid network address"}
        };
    }
}