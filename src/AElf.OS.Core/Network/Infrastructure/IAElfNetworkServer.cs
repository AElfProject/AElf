using System.Net;
using System.Threading.Tasks;

namespace AElf.OS.Network.Infrastructure;

public interface IAElfNetworkServer
{
    Task<bool> ConnectAsync(DnsEndPoint endpoint);
    Task DisconnectAsync(IPeer peer, bool sendDisconnect = false);
    Task<bool> TrySchedulePeerReconnectionAsync(IPeer peer);
    Task StartAsync();
    Task StopAsync(bool gracefulDisconnect = true);
    void CheckNtpDrift();
    Task<bool> CheckEndpointAvailableAsync(DnsEndPoint endpoint);
    Task<bool> BuildStreamForPeerAsync(IPeer peer);
}