using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

public interface IRpcServer
{
    Task Start();
    void Stop();
    bool Init(IServiceProvider scope, string rpcHost, int rpcPort);
}