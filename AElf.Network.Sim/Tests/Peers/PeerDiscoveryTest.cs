using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using AElf.Network.Sim.Node;
using Newtonsoft.Json;

namespace AElf.Network.Sim.Tests.Peers
{
    public class PeerDiscoveryTest : INetworkTest
    {
        private const string ConfigFilePath = @"Tests/Peers/layout.json";

        private readonly List<ProcNetNode> _nodes = new List<ProcNetNode>();
        
        private readonly List<KeyValuePair<int, int>> _expectedConnections = new List<KeyValuePair<int, int>>();

        public void Run()
        {
            _expectedConnections.Add(new KeyValuePair<int, int>(6780, 6781));
            _expectedConnections.Add(new KeyValuePair<int, int>(6781, 6780));
            
            try
            {
                // read JSON directly from a file
                SimulationConfig o1 = JsonConvert.DeserializeObject<SimulationConfig>(File.ReadAllText(ConfigFilePath));
                
                foreach (var nodeConf in o1.Nodes)
                {
                    ProcNetNode netNode = new ProcNetNode(nodeConf);
                    
                    netNode.EventReceived += NetNodeOnEventReceived;
                    netNode.Start();
                    
                    _nodes.Add(netNode);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("Exception while peer discovery test " + e);
            }
        }

        private void NetNodeOnEventReceived(object sender, EventArgs e)
        {
            if (sender is ProcNetNode node && e is EventReceivedArgs args)
            {
                NetEvent evt = JsonConvert.DeserializeObject<NetEvent>(args.Message);
            
                if (evt != null)
                {
                    int sourcePort = node.Port;

                    int amount = _expectedConnections.RemoveAll(m => m.Key == sourcePort && m.Value == evt.Peer.Port);
                    
                    if (amount > 0)
                        Console.WriteLine($"Removed: {sourcePort} - {evt.Peer.Port}");
                                  
                    if (!_expectedConnections.Any())
                    {
                        Program.testSuccessEvent.Set();
                    }
                }
            }
        }

        public void StopAndClean()
        {
            foreach (var node in _nodes)
            {
                node.Stop();
            }
        }
    }
}