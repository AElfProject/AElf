using System;
using System.Threading.Tasks;
public interface IRpcServer
{
    Task Start();
    void Stop();
    bool Init(IServiceProvider scope, string rpcHost, int rpcPort);
}