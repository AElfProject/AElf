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
                    else if (msgType == AElfProtocolMsgType.TxRequest)
                    {
                        await HandleTxRequest(message, args.PeerMessage);
                    }
                }
            }
            catch (Exception e)
            {
                _logger?.Trace(e, "Error while dequeuing.");
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
                    _logger?.Trace($"Block not found {breq.Id.ToByteArray().ToHex()}");
                    return;
                }
                    
                
                Message req = NetRequestFactory.CreateMessage(AElfProtocolMsgType.Block, b.ToByteArray());
                
                if (message.HasId)
                    req.Id = message.Id;

                args.Peer.EnqueueOutgoing(req);

                _logger?.Trace("Send block " + b.GetHash().DumpHex() + " to " + args.Peer);
            }
            catch (Exception e)
            {
                _logger?.Trace(e, "Error while during HandleBlockRequest.");
            }
        }

        private async Task HandleTxRequest(Message message, PeerMessageReceivedArgs args)
        {
            if (message.Payload == null || message.Payload.Length <= 0)
            {
                _logger?.Warn("Payload null or empty, cannot process transaction request.");
                return;
            }
                
            try
            {
                TxRequest breq = TxRequest.Parser.ParseFrom(message.Payload);

                if (!breq.TxHashes.Any())
                {
                    _logger?.Warn("Received transaction request with empty hash list.");
                    return;
                }

                TransactionList txList = new TransactionList();
                foreach (var txHash in breq.TxHashes)
                {
                    var hash = txHash.ToByteArray();
                    var tx = await _handler.GetTransaction(Hash.LoadByteArray(hash));
                
                    if(tx != null)
                        txList.Transactions.Add(tx);
                    else
                    {
                        _logger?.Trace("wanna get tx: " + txHash.ToByteArray().ToHex());
                    }
                }

                if (!txList.Transactions.Any())
                {
                    _logger?.Warn("None of the transactions where found.");
                    return;
                }

                byte[] serializedTxList = txList.ToByteArray();
                Message req = NetRequestFactory.CreateMessage(AElfProtocolMsgType.Transactions, serializedTxList);
                _logger?.Trace("payload length: " + req.Length);

                if (message.HasId)
                {
                    req.HasId = true;
                    req.Id = message.Id;
                }
                
                args.Peer.EnqueueOutgoing(req);
                
                _logger?.Trace("Send " + txList.Transactions.Count + " to " + args.Peer);
            }
            catch (Exception e)
            {
                _logger?.Error(e, $"Transaction request failed.");
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

            var bh = block.GetHash().DumpHex();
            _logger?.Trace(
                $"Broadcasted block \"{bh}\" to peers with {block.Body.TransactionsCount} tx(s). Block height: [{block.Header.Index}].");

            return true;
        }
    }
}