using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

public interface IRpcServer
{
    Task StartAsync();
    Task StopAsync();
    bool Init(IServiceProvider scope, string rpcHost, int rpcPort);
}