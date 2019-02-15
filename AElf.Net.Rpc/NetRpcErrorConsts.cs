using System.Collections.Generic;

namespace AElf.Net.Rpc
{
    public class NetRpcErrorConsts
    {
        public const long InvalidNetworkAddress = 30001;

        public static readonly Dictionary<long, string> RpcErrorMessage = new Dictionary<long, string>
        {
            {InvalidNetworkAddress, "Invalid network address"}
        };
    }
}