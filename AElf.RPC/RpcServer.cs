using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Autofac;

namespace AElf.RPC
{
    public class RpcServer
    {
        private IWebHost _host;

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