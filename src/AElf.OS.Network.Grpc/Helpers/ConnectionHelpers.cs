using System;
using AElf.Kernel;

namespace AElf.OS.Network.Grpc
{
    public static class ConnectionHelpers
    {
        public static PeerInfo ToPeerInfo(this ConnectionInfo connectionInfo, bool isInbound)
        {
            return new PeerInfo
            {
                Pubkey = connectionInfo.Pubkey.ToHex(),
                ProtocolVersion = connectionInfo.Version,
                ConnectionTime = TimestampHelper.GetUtcNow().Seconds,
                IsInbound = isInbound
            };
        }
    }
}