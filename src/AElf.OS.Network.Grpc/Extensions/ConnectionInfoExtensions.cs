using AElf.Kernel;
using AElf.OS.Network.Protocol.Types;

namespace AElf.OS.Network.Grpc.Extensions
{
    public static class ConnectionInfoExtensions
    {
        public static PeerConnectionInfo ToPeerInfo(this Network.ConnectionInfo connectionInfo, bool isInbound)
        {
            return new PeerConnectionInfo
            {
                Pubkey = connectionInfo.Pubkey.ToHex(),
                ProtocolVersion = connectionInfo.Version,
                ConnectionTime = TimestampHelper.GetUtcNow().Seconds,
                IsInbound = isInbound
            };
        }
    }
}