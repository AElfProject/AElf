using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AElf.Network.Data;
using AElf.Network.Peers;
using Google.Protobuf;

namespace AElf.Kernel.Node.Protocol
{
    public class AElfProtocolDirector : IProtocolDirector
    {
        private IPeerManager _peerManager;
        private List<PendingRequest> _resetEvents = new List<PendingRequest>();

        private BlockSynchronizer _blockSynchronizer;
        
        private MainChainNode _node;

        public AElfProtocolDirector(IPeerManager peerManager)
        {
            _peerManager = peerManager;
            
        }
        
        public void Start()
        {
            //_blockSynchronizer.Init();
            _peerManager.Start();
            _peerManager.MessageReceived += ProcessPeerMessage;
        }
        
        /// <summary>
        /// Temporary solution, this is used for injecting a
        /// reference to the node.
        /// todo : remove dependency on the node
        /// </summary>
        /// <param name="node"></param>
        public void SetCommandContext(MainChainNode node, bool doSync = false)
        {
            _node = node;
            
            if (doSync)
                _blockSynchronizer = new BlockSynchronizer(_node, _peerManager); // todo move
        }

        public void AddTransaction(Transaction tx)
        {
            _blockSynchronizer.SetTransaction(tx.GetHash().ToByteArray());
        }

        public List<NodeData> GetPeers(ushort? numPeers)
        {
            return _peerManager.GetPeers(numPeers);
        }
        
        public async Task BroadcastTransaction(ITransaction tx)
        {
            byte[] transaction = tx.Serialize();
            
            var pendingRequest = BuildRequest();
            
            bool success 
                = await _peerManager.BroadcastMessage(MessageTypes.BroadcastTx, transaction, pendingRequest.Id);
            
            /*if (success)
                _resetEvents.Add(pendingRequest);

            pendingRequest.ResetEvent.WaitOne();*/
        }
        
        public async Task BroadcastBlock(Block block)
        {
            byte[] serializedBlock = block.ToByteArray();
            
            bool success 
                = await _peerManager.BroadcastMessage(MessageTypes.BroadcastBlock, serializedBlock, 0);
        }
        
        #region Response handling
        
        /// <summary>
        /// Dispatch callback that is executed when a peer receives a message.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void ProcessPeerMessage(object sender, EventArgs e)
        {
            if (sender != null && e is MessageReceivedArgs args && args.Message != null)
            {
                AElfPacketData message = args.Message;

                if (message.MsgType == (int)MessageTypes.BroadcastTx || message.MsgType == (int)MessageTypes.SendTx)
                {
                    var fromSend = message.MsgType == (int) MessageTypes.SendTx;
                    await _node.ReceiveTransaction(message.Payload, fromSend);
                }
                else if (message.MsgType == (int)MessageTypes.BroadcastBlock)
                {
                    try
                    {
                        Block b = Block.Parser.ParseFrom(message.Payload);
                        await _blockSynchronizer.AddBlockToSync(b);
                    }
                    catch (Exception exception)
                    {
                        ;
                    }
                }
                else if (message.MsgType == (int)MessageTypes.RequestBlock)
                {
                    // Get the requested blocks, send it back to the peer
                }
                else if (message.MsgType == (int)MessageTypes.SendBlock)
                {
                    // Reception a block
                    Block b = Block.Parser.ParseFrom(message.Payload);
                    await _blockSynchronizer.AddBlockToSync(b);
                }
                else if (message.MsgType == (int)MessageTypes.Height)
                {
                    HeightData height = HeightData.Parser.ParseFrom(message.Payload);
                    _blockSynchronizer.SetPeerHeight(args.Peer, height.Height);
                }
                else if (message.MsgType == (int)MessageTypes.HeightRequest)
                {
                    int height = _node.GetCurrentChainHeight();
                    HeightData data = new HeightData { Height = height };
                    var req = NetRequestFactory.CreateRequest(MessageTypes.Height, data.ToByteArray(), 0);
                    await args.Peer.SendAsync(req.ToByteArray());
                }
                
                // Process any messages
                
                ClearResetEvent(message.Id);
            }
        }

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