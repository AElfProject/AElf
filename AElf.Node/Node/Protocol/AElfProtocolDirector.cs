using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using AElf.Common.ByteArrayHelpers;
using AElf.Network;
using AElf.Network.Connection;
using AElf.Network.Data;
using AElf.Network.Data.Protobuf;
using AElf.Network.Peers;
using Google.Protobuf;
using NLog;
using NodeData = AElf.Network.Data.Protobuf.NodeData;

namespace AElf.Kernel.Node.Protocol
{
    public class AElfProtocolDirector : IProtocolDirector
    {
        private INetworkManager _netManager;
        private List<PendingRequest> _resetEvents = new List<PendingRequest>();

        private BlockSynchronizer _blockSynchronizer;
        
        private MainChainNode _node;

        private ILogger _logger;

        private BlockingCollection<Message> _messages;

        public AElfProtocolDirector(INetworkManager netManager)
        {
            _messages = new BlockingCollection<Message>();
            _netManager = netManager;
            _logger = LogManager.GetLogger("ProtocolDirector");
        }
        
        public void Start()
        {
            _netManager.Start();
            _netManager.MessageReceived += ProcessPeerMessage;
        }

        /// <summary>
        /// Temporary solution, this is used for injecting a
        /// reference to the node.
        /// todo : remove dependency on the node
        /// </summary>
        /// <param name="node"></param>
        /// <param name="isGenerator"></param>
        public void SetCommandContext(MainChainNode node, bool isGenerator = false)
        {
            _node = node;
            
            ulong height = _node.GetCurrentChainHeight().Result;
                
            _blockSynchronizer = new BlockSynchronizer(_netManager, _node); // todo move
            
            if (!isGenerator)
            {
                _blockSynchronizer.SyncFinished += BlockSynchronizerOnSyncFinished;
            }
            else
            {
                StartMining();
            }
            
            Task.Run(() => _blockSynchronizer.Start(!isGenerator));
        }

        private void BlockSynchronizerOnSyncFinished(object sender, EventArgs eventArgs)
        {
            StartMining();
        }

        private void StartMining()
        {
            if (_node.IsMiner() && !_node.IsMining)
            {
                _node.DoDPos();
            }
        }

        public void AddTransaction(Transaction tx)
        {
            //_blockSynchronizer.EnqueueJob(new Job { Transaction = tx });
        }

        public List<NodeData> GetPeers(ushort? numPeers)
        {
            return new List<NodeData>();
        }
        
        public async Task<int> BroadcastTransaction(ITransaction tx)
        {
            byte[] transaction = tx.Serialize();
            
            var pendingRequest = BuildRequest();
            
            int broadcastCount 
                = await _netManager.BroadcastMessage(MessageType.BroadcastTx, transaction);

            return broadcastCount;

            /*if (success)
                _resetEvents.Add(pendingRequest);

            pendingRequest.ResetEvent.WaitOne();*/
        }
        
        public async Task<int> BroadcastBlock(Block block)
        {
            byte[] serializedBlock = block.ToByteArray();
            return await _netManager.BroadcastMessage(MessageType.BroadcastBlock, serializedBlock);
        }
        
        public long GetLatestIndexOfOtherNode()
        {
            var currentPendingBlocks = _blockSynchronizer.PendingBlocks.ToList();
            if (currentPendingBlocks == null || currentPendingBlocks.Count <= 0)
            {
                return -1;
            }

            return (long) (from pendingBlock in currentPendingBlocks
                orderby pendingBlock.Block.Header.Index descending
                select pendingBlock.Block.Header.Index).First();
        }

        public void IncrementChainHeight()
        {
            Interlocked.Increment(ref _blockSynchronizer.CurrentExecHeight);
        }

        #region Response handling
        
        
        
        /// <summary>
        /// Dispatch callback that is executed when a peer receives a message.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void ProcessPeerMessage(object sender, EventArgs e)
        {   
            if (sender != null && e is NetMessageReceived args  && args.Message != null)
            {
                Message message = args.Message;
                MessageType msgType = (MessageType)message.Type;

                if (msgType == MessageType.BroadcastTx) // || msgType == MessageType.Tx)
                {
                    await  HandleTransactionReception(message);
                }
                else if (msgType == MessageType.BroadcastBlock) // || message.Type == (int)MessageType.Block)
                {
                    // todo maybe merge the above types
                    //await HandleBlockReception(message, msgType);
                }
                else if (msgType == MessageType.RequestBlock)
                {
                    await HandleBlockRequest(message, args.PeerMessage);
                }
                else if (msgType == MessageType.Height)
                {
                    HandlePeerHeightReception(message, args.PeerMessage);
                }
                else if (msgType == MessageType.HeightRequest)
                {
                    await HandleHeightRequest(message, args.PeerMessage);
                }
                else if (msgType == MessageType.TxRequest)
                {
                    await HandleTxRequest(message, args.PeerMessage);
                }
            }
        }
        
        private async Task HandleTxRequest(Message message, PeerMessageReceivedArgs args)
        {
            string hash = null;
            
            try
            {
                TxRequest breq = TxRequest.Parser.ParseFrom(message.Payload);

                hash = breq.TxHash.ToByteArray().ToHex();
                
                ITransaction tx = await _node.GetTransaction(breq.TxHash);

                if (!(tx is Transaction t))
                {
                    _logger?.Trace("Could not find transaction: ", hash);
                    return;
                }
                
                var req = NetRequestFactory.CreateMessage(MessageType.Tx, t.ToByteArray());
                args.Peer.EnqueueOutgoing(req);

                _logger?.Trace("Send tx " + t.GetHash().ToHex() + " to " + args.Peer + "(" + t.ToByteArray().Length +
                               " bytes)");
            }
            catch (Exception e)
            {
                _logger?.Trace(e, $"Transaction request failed. Hash : {hash}");
            }
        }

        internal async Task HandleBlockRequest(Message message, PeerMessageReceivedArgs args)
        {
            try
            {
                BlockRequest breq = BlockRequest.Parser.ParseFrom(message.Payload);
                Block block = await _node.GetBlockAtHeight(breq.Height);

                var req = NetRequestFactory.CreateMessage(MessageType.Block, block.ToByteArray());
                args.Peer.EnqueueOutgoing(req);
                _logger?.Trace("Send block " + block.GetHash().ToHex() + " to " + args.Peer);
            }
            catch (Exception e)
            {
                _logger?.Trace(e, "Error while during HandleBlockRequest.");
            }
        }

        internal async Task HandleHeightRequest(Message message, PeerMessageReceivedArgs args)
        {
            try
            {
                ulong height = await _node.GetCurrentChainHeight();
                HeightData data = new HeightData { Height = (int)height };
                var req = NetRequestFactory.CreateMessage(MessageType.Height, data.ToByteArray());
                args.Peer.EnqueueOutgoing(req);
            }
            catch (Exception e)
            {
                _logger?.Trace(e, "Error while during HandleHeightRequest.");
            }
        }

        internal void HandlePeerHeightReception(Message message, PeerMessageReceivedArgs args)
        {
            try
            {
                HeightData height = HeightData.Parser.ParseFrom(message.Payload);
                //_blockSynchronizer.SetPeerHeight(args.Peer, height.Height);
            }
            catch (Exception e)
            {
                _logger?.Trace(e, "Error while during HandlePeerHeightReception.");
            }
        }

        internal async Task HandleTransactionReception(Message message)
        {
            try
            {
                var fromSend = message.Type == (int) MessageType.Tx;
                
                await _node.ReceiveTransaction(message.Payload, fromSend);
            }
            catch (Exception e)
            {
                _logger?.Trace(e, "Error while receiving transaction.");
            }
        }

//        internal async Task HandleBlockReception(Message message, MessageType type)
//        {
//            try
//            {
//                Block b = Block.Parser.ParseFrom(message.Payload);
//                
//                _logger?.Trace("Block received: " + b.GetHash().ToHex());
//                _blockSynchronizer.EnqueueJob(new Job { Block = b });
//
//                /*if (types == MessageTypes.BroadcastBlock)
//                {
//                    
//                    await _blockSynchronizer.AddBlockToSync(b);
//                }
//                else
//                {
//                    // Block sent to answer a request
//                    await _blockSynchronizer.AddRequestedBlock(b);
//                }*/
//            }
//            catch (Exception e)
//            {
//                _logger?.Trace(e, "Error while receiving HandleBlockReception.");
//            }
//        }

        private void ClearResetEvent(int eventId)
        {
            var resetEvent = _resetEvents.FirstOrDefault(p => p.Id == eventId);

            if (resetEvent != null)
            {
                resetEvent.ResetEvent.Set();
                _resetEvents.Remove(resetEvent);
            }
        }
        
        #endregion

        #region Requests

        
        #endregion

        #region Helpers
        
        private PendingRequest BuildRequest()
        {
            int id = new Random().Next();
            AutoResetEvent resetEvt = new AutoResetEvent(false);

            return new PendingRequest {Id = id, ResetEvent = resetEvt};
        }

        #endregion
    }
}