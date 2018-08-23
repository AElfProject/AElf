using System.Threading.Tasks;
using Autofac;

public interface IRpcServer
{
    Task Start();
    void Stop();
    bool Init(ILifetimeScope scope, string rpcHost, int rpcPort);
}