using System;
using System.Collections.Generic;
using System.Linq;
using AElf.Common;
using AElf.Net.Rpc;
using AElf.Node;
using AElf.Node.AElfChain;
using AElf.RPC;

namespace AElf.Network.Sim.Node
{
    class Program
    {
        static void Main(string[] args)
        {
            /*
            NodeConfiguration confContext = new NodeConfiguration();
            confContext.WithRpc = true;
    
            var builder = new ContainerBuilder();
            
            builder.RegisterModule(new RpcAutofacModule());
            builder.RegisterModule(new NetRpcAutofacModule());
            builder.RegisterModule(new NodeAutofacModule());
            builder.RegisterModule(new NetworkAutofacModule());
                
            IContainer container = null;
            
            try
            {
                container = builder.Build();
            }
            catch (Exception e)
            {
                Console.WriteLine($"container build failed: {e}");
            }
            
            if (container == null)
            {
                Console.WriteLine("IoC setup failed");
            }
            else
            {
                using (var scope = container.BeginLifetimeScope())
                {
                    var rpc = scope.Resolve<IRpcServer>();
                    NetworkConfig.Instance.ListeningPort = int.Parse(args[1]);
                    Console.WriteLine("rpc param : " + int.Parse(args[0]));
                    rpc.Init(scope, "localhost", int.Parse(args[0]));
                    
                    if(args.Length > 2)
                        Console.WriteLine("btnds:" + args[2]);

                    List<string> btnds = new List<string>();
                    for (int i = 2; i < args.Length; i++)
                    {
                        btnds.Add(args[i]);
                        Console.WriteLine("Bootnode: " + args[i]);
                    }

                    if (btnds.Any())
                        NetworkConfig.Instance.Bootnodes = btnds;
                    
                    var node = scope.Resolve<INode>();
                    node.Initialize(confContext);
                    node.Start();
                }
            }

            Console.ReadKey();*/
        }
    }
}