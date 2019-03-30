using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using AElf.Common;
using AElf.Kernel;
using AElf.OS.Network.Application;
using AElf.OS.Network.Infrastructure;
using Grpc.Core;

namespace AElf.OS.Network.Grpc
{
    public class GrpcPeer : IPeer
    {
        public event EventHandler DisconnectionEvent;

        private readonly Channel _channel;
        private readonly PeerService.PeerServiceClient _client;

        /// <summary>
        /// Property that describes a valid state. Valid here means that the peer is ready to be used for communication.
        /// </summary>
        public bool IsReady
        {
            get { return _channel.State == ChannelState.Idle || _channel.State == ChannelState.Ready; }
        }

        public Hash CurrentBlockHash { get; private set; }
        public long CurrentBlockHeight { get; private set; }
        public string PeerIpAddress { get; }
        public string PubKey { get; }

        public IReadOnlyDictionary<long, Hash> RecentBlockHeightAndHashMappings { get; }

        private readonly ConcurrentDictionary<long, Hash> _recentBlockHeightAndHashMappings;

        public GrpcPeer(Channel channel, PeerService.PeerServiceClient client, string pubKey, string peerIpAddress)
        {
            _channel = channel;
            _client = client;

            PeerIpAddress = peerIpAddress;

            PubKey = pubKey;

            _recentBlockHeightAndHashMappings = new ConcurrentDictionary<long, Hash>();
            RecentBlockHeightAndHashMappings = new ReadOnlyDictionary<long, Hash>(_recentBlockHeightAndHashMappings);
        }

        public async Task<Block> RequestBlockAsync(Hash hash)
        {
            var blockRequest = new BlockRequest {Hash = hash};

            var blockReply = await RequestAsync(_client, c => c.RequestBlockAsync(blockRequest),
                $"Block request for {hash} failed.");

            return blockReply?.Block;
        }

        public async Task<List<Block>> GetBlocksAsync(Hash firstHash, int count)
        {
            var blockRequest = new BlocksRequest {PreviousBlockHash = firstHash, Count = count};

            var list = await RequestAsync(_client, c => c.RequestBlocksAsync(blockRequest),
                $"Get blocks for {{ first: {firstHash}, count: {count} }} failed.");

            if (list == null)
                return new List<Block>();

            return list.Blocks.ToList();
        }

        public async Task AnnounceAsync(PeerNewBlockAnnouncement header)
        {
            await RequestAsync(_client, c => c.AnnounceAsync(header),
                $"Bcast announce for {header.BlockHash} failed.");
        }

        public async Task SendTransactionAsync(Transaction tx)
        {
            await RequestAsync(_client, c => c.SendTransactionAsync(tx),
                $"Bcast tx for {tx.GetHash()} failed.");
        }

        private async Task<TResp> RequestAsync<TResp>(PeerService.PeerServiceClient client,
            Func<PeerService.PeerServiceClient, AsyncUnaryCall<TResp>> func, string errorMessage)
        {
            try
            {
                return await func(client);
            }
            catch (RpcException e)
            {
                HandleFailure(e, errorMessage);
            }

            return default(TResp);
        }

        /// <summary>
        /// This method handles the case where the peer is potentially down. If the Rpc call
        /// put the channel in TransientFailure or Connecting, we give the connection a certain time to recover.
        /// </summary>
        private void HandleFailure(RpcException rpcException, string errorMessage)
        {
            // If channel has been shutdown (unrecoverable state) remove it.
            if (_channel.State == ChannelState.Shutdown)
            {
                DisconnectionEvent?.Invoke(this, EventArgs.Empty);
                return;
            }

            if (_channel.State == ChannelState.TransientFailure || _channel.State == ChannelState.Connecting)
            {
                Task.Run(async () =>
                {
                    await _channel.TryWaitForStateChangedAsync(_channel.State,
                        DateTime.UtcNow.AddSeconds(NetworkConsts.DefaultPeerDialTimeout));

                    // Either we connected again or the state change wait timed out.
                    if (_channel.State == ChannelState.TransientFailure || _channel.State == ChannelState.Connecting)
                    {
                        await StopAsync();
                        DisconnectionEvent?.Invoke(this, EventArgs.Empty);
                    }
                });
            }
            else
            {
                throw new NetworkException($"Failed request to {this}: {errorMessage}", rpcException);
            }
        }

        public async Task StopAsync()
        {
            try
            {
                await _channel.ShutdownAsync();
            }
            catch (InvalidOperationException)
            {
                // If channel already shutdown
            }
        }

        public void HandlerRemoteAnnounce(PeerNewBlockAnnouncement peerNewBlockAnnouncement)
        {
            CurrentBlockHeight = peerNewBlockAnnouncement.BlockHeight;
            CurrentBlockHash = peerNewBlockAnnouncement.BlockHash;
            _recentBlockHeightAndHashMappings[CurrentBlockHeight] = CurrentBlockHash;
            while (_recentBlockHeightAndHashMappings.Count > 10)
            {
                _recentBlockHeightAndHashMappings.TryRemove(_recentBlockHeightAndHashMappings.Keys.Min(), out _);
            }
        }

        public async Task SendDisconnectAsync()
        {
            await _client.DisconnectAsync(new DisconnectReason {Why = DisconnectReason.Types.Reason.Shutdown});
        }

        public override string ToString()
        {
            return $"{{ listening-port: {PeerIpAddress}, key: {PubKey.Substring(0, 45)}... }}";
        }
    }
}