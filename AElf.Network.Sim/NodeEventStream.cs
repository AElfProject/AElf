using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR.Client;

namespace AElf.Network.Sim
{
    public class EventReceivedArgs : EventArgs
    {
        public string Namespace { get; set; }
        public string Message { get; set; }
    }
    
    public class NodeEventStream
    {
        HubConnection _connection;
        
        private readonly int _port;
        private readonly string _ns;

        public event EventHandler EventReceived;
    
        public NodeEventStream(int port, string ns)
        {
            _port = port;
            _ns = ns;
        }
    
        public async Task StartAsync()
        {
            try
            {
                _connection = new HubConnectionBuilder()
                    .WithUrl($"http://localhost:{_port}/events/{_ns}")
                    .Build();

                await _connection.StartAsync();

                Console.WriteLine($"Listening to {_ns} events...");
        
                var cts = new CancellationTokenSource();
                Console.CancelKeyPress += (sender, a) =>
                {
                    a.Cancel = true;
                    cts.Cancel();
                };

                _connection.Closed += e =>
                {
                    Console.WriteLine("Event stream was terminated.");

                    cts.Cancel();
                    return Task.CompletedTask;
                };
        
                _connection.On("event", (string s) =>
                {
                    Console.WriteLine($"{s}");
                    EventReceived?.Invoke(this, new EventReceivedArgs
                    {
                        Namespace = _ns,
                        Message = s
                    });
                });
            }
            catch (Exception e)
            {
                ;
            }
        }

        public async Task StopAsync()
        {
            await _connection.StopAsync();
        }
    }
}