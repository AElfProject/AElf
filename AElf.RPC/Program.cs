using System;
using Grpc.Core;

namespace AElf.RPC
{
    class Program
    {
        static void Main(string[] args)
        {
            const int Port = 50052;
            
            Console.WriteLine("RPC server listening on port " + Port);
            // create a server
            var server = Startup(Port);
            
            Console.WriteLine("Press any key to stop the server...");
            Console.ReadKey();
            server.ShutdownAsync().Wait();
            
        }

        static Server Startup(int port)
        {
            Server server = new Server
            {
                Services = { AElfRPC.BindService(new SmartContractExecution()) },
                Ports = { new ServerPort("localhost", port, ServerCredentials.Insecure) }
            };
            server.Start();
            return server;
        }
    }

}