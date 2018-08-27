using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR.Client;

namespace AElf.CLI.Streaming
{
    public class EventMonitor
    {
        private readonly int _port;
        private readonly string _ns;
        
        public EventMonitor(int port, string ns)
        {
            _port = port;
            _ns = ns;
        }
        
        public async Task Start()
        {
            try
            {
                var connection = new HubConnectionBuilder()
                    .WithUrl($"http://localhost:{_port}/events/{_ns}")
                    .Build();

                await connection.StartAsync();

                Console.WriteLine($"Listening to {_ns} events...");
            
                var cts = new CancellationTokenSource();
                Console.CancelKeyPress += (sender, a) =>
                {
                    a.Cancel = true;
                    cts.Cancel();
                };

                connection.Closed += e =>
                {
                    Console.WriteLine("Event stream was terminated.");

                    cts.Cancel();
                    return Task.CompletedTask;
                };
            
                connection.On("event", (string s) =>
                {
                    Console.WriteLine($"{s}");
                });
            }
            catch (Exception e)
            {
                ;
            }
        }
    }
}