using System.Threading.Tasks;
using Akka.Actor;

namespace AElf.Execution
{
    public interface IActorEnvironment
    {
        IActorRef Requestor { get; }
        bool Initialized { get; }
        Task TerminationHandle { get; }
        void InitActorSystem();
        Task StopAsync();
    }
}