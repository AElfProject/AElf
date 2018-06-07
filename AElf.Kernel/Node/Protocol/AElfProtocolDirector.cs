using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AElf.Network.Data;
using AElf.Network.Peers;

namespace AElf.Kernel.Node.Protocol
{
    public class AElfProtocolDirector : IProtocolDirector
    {
        private IPeerManager _peerManager;
        private List<PendingRequest> _resetEvents = new List<PendingRequest>();
        
        private MainChainNode _node;

        public AElfProtocolDirector(IPeerManager peerManager)
        {
            _peerManager = peerManager;
        }
        
        public void Start()
        {
            _peerManager.Start();
            _peerManager.MessageReceived += ProcessPeerMessage;
        }
        
        /// <summary>
        /// Temporary solution, this is used for injecting a
        /// reference to the node.
        /// todo : remove dependency on the node
        /// </summary>
        /// <param name="node"></param>
        public void SetCommandContext(MainChainNode node)
        {
            _node = node;
        }

        public List<NodeData> GetPeers(ushort numPeers)
        {
            return _peerManager.GetPeers(numPeers);
        }
        
        public async Task BroadcastTransaction(ITransaction tx)
        {
            byte[] transaction = tx.Serialize();
            
            var pendingRequest = BuildRequest();
            
            bool success 
                = await _peerManager.BroadcastMessage(MessageTypes.BroadcastTx, transaction, pendingRequest.Id);
            
            if (success)
                _resetEvents.Add(pendingRequest);

            pendingRequest.ResetEvent.WaitOne();
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