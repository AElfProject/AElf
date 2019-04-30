using System;
using System.Threading.Tasks;
using AElf.Kernel.Types.Common;
using AElf.Network;
using AElf.Network.Peers;
using AElf.RPC.Hubs.Net;
using Microsoft.AspNetCore.Hosting;
using Autofac;
using Easy.MessageHub;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using NLog;

namespace AElf.RPC
{
    public class RpcServer : IRpcServer
    {
        private IWebHost _host;
        private readonly ILogger _logger;

        public RpcServer(ILogger logger)
        {
            _logger = logger;
            
            MessageHub.Instance.Subscribe<TerminationSignal>(signal =>
            {
                if (signal.Module == TerminatedModuleEnum.Rpc)
                {
                    Stop();
                    MessageHub.Instance.Publish(new TerminatedModule(TerminatedModuleEnum.Rpc));
                }
            });
        }

        public bool Init(ILifetimeScope scope, string rpcHost, int rpcPort)
        {
            try
            {
                var url = "http://" + rpcHost + ":" + rpcPort;

                _host = new WebHostBuilder()
                    .UseKestrel(options =>
                        {
                            options.Limits.KeepAliveTimeout = TimeSpan.FromSeconds(20);
                            options.Limits.MaxConcurrentConnections = 100;
                            options.Limits.MaxConcurrentUpgradedConnections = 100;
                            //options.Limits.MaxRequestBodySize = 10 * 1024;
                        }
                    )
                    .UseUrls(url)
                    .ConfigureServices(sc =>
                    {
                        sc.AddCors();
                        sc.AddSingleton(scope.Resolve<INetworkManager>());
                        sc.AddSingleton(scope.Resolve<IPeerManager>());

                        sc.AddSignalRCore();
                        sc.AddSignalR();

                        sc.AddScoped<NetContext>();

                        RpcServerHelpers.ConfigureServices(sc, scope);
                    })
                    .Configure(ab =>
                    {
                        ab.UseCors(builder => { builder.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod(); });
                        ab.UseSignalR(routes => { routes.MapHub<NetworkHub>("/events/net"); });

                        RpcServerHelpers.Configure(ab, scope);
                    })
                    .Build();

                _host.Services.GetService<NetContext>();
            }
            catch (Exception e)
            {
                _logger?.Error(e, "Exception while RPC server init.");
                return false;
            }

            return true;
        }

        public async Task Start()
        {
            try
            {
                _logger?.Info("RPC server start.");
                await _host.RunAsync();
            }
            catch (Exception e)
            {
                _logger?.Error(e, "Exception while start RPC server.");
            }
        }

        public void Stop()
        {
             _host.StopAsync();
        }
    }
}