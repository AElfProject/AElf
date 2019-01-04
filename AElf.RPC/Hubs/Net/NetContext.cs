using System;
using System.Collections.Generic;
using AElf.Network.Eventing;
using AElf.Network.Peers;
using Microsoft.AspNetCore.SignalR;
using Newtonsoft.Json;

namespace AElf.RPC.Hubs.Net
{
    public class NetContext: IDisposable
    {
        private readonly IHubContext<NetworkHub> _hubContext;
        private readonly IPeerManager _peerManager;

        public NetContext(IHubContext<NetworkHub> hubContext, IPeerManager peerManager)
        {
            _hubContext = hubContext;
            _peerManager = peerManager;
            peerManager.PeerEvent += ManagerOnMessageReceived;
        }

        private void ManagerOnMessageReceived(object sender, EventArgs e)
        {
            if (e is PeerEventArgs peerAdded)
            {
                PublishEvent("net", JsonConvert.SerializeObject(peerAdded));
            }
        }

        public void PublishEvent(string ns, string evt)
        {
            try
            {
                List<string> groups = new List<string> { ns };
                _hubContext.Clients.Groups(groups).SendAsync("event", evt);
            }
            catch (Exception e)
            {
                Console.WriteLine("Exception while publish event: " + e.Message);
            }
        }

        public void Dispose()
        {
            
            _peerManager.PeerEvent -= ManagerOnMessageReceived;
        }
    }
}