using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Autofac;

namespace AElf.RPC
{
    public class RpcServer : IRpcServer
    {
        private IWebHost _host;

        public bool Init(ILifetimeScope scope, string rpcHost, int rpcPort)
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
                return false;
            }

            return true;
        }

        public async Task Start()
        {
            await _host.RunAsync();
        }

        public void Stop()
        {
            _host.StopAsync();
        }
    }
}