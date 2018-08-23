using System.Threading.Tasks;

public interface IRpcServer
{
    Task Start();
    void Stop();
}