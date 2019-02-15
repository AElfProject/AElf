using System.Threading.Tasks;
using AElf.OS.Jobs;
using Volo.Abp.BackgroundJobs;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus;

namespace AElf.OS.Handlers
{
    public class PeerConnectedEventHandler : ILocalEventHandler<PeerConnectedEventData>, ISingletonDependency
    {
        public IBackgroundJobManager BackgroundJobManager { get; set; }
            
        public Task HandleEventAsync(PeerConnectedEventData eventData)
        {
            BackgroundJobManager.EnqueueAsync(new ForkDownloadJobArgs());
            return Task.CompletedTask;
        }
    }
}