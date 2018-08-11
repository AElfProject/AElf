using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Autofac;
using NLog;

namespace AElf.RPC
{
    public class RpcServer
    {
        private readonly ILogger _logger;
        private IWebHost _host;

        public RpcServer(ILogger logger)
        {
            _logger = logger;
        }

        public bool Initialize(ILifetimeScope scope, string rpcHost, int rpcPort)
        {
            try
            {
                var url = "http://" + rpcHost + ":" + rpcPort;
                _host = new WebHostBuilder()
                    .UseKestrel()
                    .UseUrls(url)
                    .ConfigureServices(sc => RpcServerHelpers.ConfigureServices(sc, scope))
                    .Configure(ab => RpcServerHelpers.Configure(ab, scope))
                    .Build();
            }
            catch (Exception e)
            {
                _logger.Log(LogLevel.Error, e, "Error while starting the RPC server.");
                return false;
            }

            return true;
        }

        public async Task RunAsync()
        {
            await _host.RunAsync();
        }
    }
}