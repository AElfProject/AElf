using System;
using System.Net;
using System.Threading.Tasks;
using AElf.OS.Network.Infrastructure;

namespace AElf.OS.Network.Grpc
{
    public interface IConnectionService
    {
        GrpcPeer GetPeerByPubkey(string pubkey);
        Task DisconnectAsync(IPeer peer, bool sendDisconnect = false);
        Task<bool> SchedulePeerReconnection(DnsEndPoint endpoint);
        Task<bool> TrySchedulePeerReconnectionAsync(IPeer peer);
        Task<bool> ConnectAsync(DnsEndPoint endpoint);
        Task<HandshakeReply> DoHandshakeAsync(DnsEndPoint endpoint, Handshake handshake);
        void ConfirmHandshake(string peerPubkey);
        Task DisconnectPeersAsync(bool gracefulDisconnect);
        Task<bool> CheckEndpointAvailableAsync(DnsEndPoint endpoint);
        Task RemovePeerAsync(string pubkey);
    }
}