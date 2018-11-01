using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;
using AElf.Common;
using AElf.Kernel;
using AElf.Kernel.Node;
using AElf.Network;
using AElf.Network.Connection;
using AElf.Network.Data;
using AElf.Network.Peers;
using AElf.Node.Protocol.Events;
using Google.Protobuf;
using NLog;
using NServiceKit.Common;

namespace AElf.Node
{
    public class P2P : IP2P
    {
        private readonly ILogger _logger;
        private readonly INetworkManager _netManager;

        private BlockingCollection<NetMessageReceivedEventArgs> _messageQueue =
            new BlockingCollection<NetMessageReceivedEventArgs>();

        private P2PHandler _handler;

        public P2P(P2PHandler handler, ILogger logger, INetworkManager netManager)
        {
            _handler = handler;
            _logger = logger;
            _netManager = netManager;
            _netManager.MessageReceived += ProcessPeerMessage;
        }

        public async Task ProcessLoop()
        {
            try
            {
                while (true)
                {
                    var args = _messageQueue.Take();

                    var message = args.Message;
                    var msgType = (AElfProtocolMsgType) message.Type;

                    if (msgType == AElfProtocolMsgType.RequestBlock)
                    {
                        await HandleBlockRequest(message, args.PeerMessage);
                    }
                    else if (msgType == AElfProtocolMsgType.HeaderRequest)
                    {
                        await HandleBlockHeaderRequest(message, args.PeerMessage);
                    }
                }
            }
            catch (Exception e)
            {
                _logger?.Error(e, "Error while dequeuing.");
            }
        }

        private async Task HandleBlockHeaderRequest(Message message, PeerMessageReceivedArgs args)
        {
            if (message?.Payload == null)
            {
                _logger?.Warn($"Hash request from [{args.Peer}], message/payload is null.");
                return;
            }

            try
            {
                var hashReq = BlockHeaderRequest.Parser.ParseFrom(message.Payload);
                var blockHeaderList = await _handler.GetBlockHeaderList((ulong) hashReq.Height, hashReq.Count);
                
                var req = NetRequestFactory.CreateMessage(AElfProtocolMsgType.Headers, blockHeaderList.ToByteArray());
                
                if (message.HasId)
                    req.Id = message.Id;

                args.Peer.EnqueueOutgoing(req);

                _logger?.Debug($"Send {blockHeaderList.Headers.Count} block headers start " +
                               $"from {blockHeaderList.Headers.FirstOrDefault()?.GetHash().DumpHex()}, to node {args.Peer}.");
            }
            catch (Exception e)
            {
                _logger?.Error(e, "Error while during HandleBlockRequest.");
            }
        }

        internal async Task<Block> HandleBlockRequestByHeight(int height)
        {
            if (height <= 0)
            {
                _logger?.Warn($"Cannot handle request for block because height {height} is not valid.");
                return null;
            }
                
            var block = await _handler.GetBlockAtHeight(height);

            return block;
        }
        
        internal async Task<Block> HandleBlockRequestByHash(byte[] hash)
        {
            return await _handler.GetBlockFromHash(Hash.LoadByteArray(hash));
        }
        
        internal async Task HandleBlockRequest(Message message, PeerMessageReceivedArgs args)
        {
            if (message?.Payload == null)
            {
                _logger?.Warn($"Block request from [{args.Peer}], message/payload is null.");
                return;
            }
                
            try
            {
                var breq = BlockRequest.Parser.ParseFrom(message.Payload);

                Block b;
                
                if (breq.Id != null && breq.Id.Length > 0)
                {
                    b = await HandleBlockRequestByHash(breq.Id.ToByteArray());
                }
                else
                {
                    b = await HandleBlockRequestByHeight(breq.Height);
                }

                if (b == null)
                {
                    _logger?.Warn($"Block not found {breq.Id?.ToByteArray().ToHex()}");
                    return;
                }
                    
                
                Message req = NetRequestFactory.CreateMessage(AElfProtocolMsgType.Block, b.ToByteArray());
                
                if (message.HasId)
                    req.Id = message.Id;

                args.Peer.EnqueueOutgoing(req);

                _logger?.Debug($"Send block {b.BlockHashToHex } to {args.Peer}");
            }
            catch (Exception e)
            {
                _logger?.Error(e, "Error while during HandleBlockRequest.");
            }
        }

        private void ProcessPeerMessage(object sender, EventArgs e)
        {
            if (sender != null && e is NetMessageReceivedEventArgs args && args?.PeerMessage?.Peer != null && args.Message != null)
            {
                _messageQueue.Add(args);
            }
        }

        public async Task<bool> BroadcastBlock(IBlock block)
        {
            if (!(block is Block b))
            {
                return false;
            }

            var serializedBlock = b.ToByteArray();
            await _netManager.BroadcastBlock(block.GetHash().Value.ToByteArray(), serializedBlock);

            _logger?.Trace($"Broadcasted block {block.BlockHashToHex} to peers " +
                           $"with {block.Body.TransactionsCount} tx(s). Block height: [{block.Header.Index}].");

            return true;
        }
    }
}