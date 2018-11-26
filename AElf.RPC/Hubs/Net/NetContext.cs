using System;
using System.Collections.Generic;
using AElf.Network.Eventing;
using AElf.Network.Peers;
using Microsoft.AspNetCore.SignalR;
using Newtonsoft.Json;

namespace AElf.RPC.Hubs.Net
{
    public class NetContext
    {
        private readonly IHubContext<NetworkHub> _hubContext;

        public NetContext(IHubContext<NetworkHub> hubContext, IPeerManager peerManager)
        {
            _hubContext = hubContext;
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
    }
}