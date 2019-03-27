using System.Threading.Tasks;
using AElf.Kernel.Blockchain.Events;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Volo.Abp.BackgroundJobs;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus;

namespace AElf.Kernel
{
    public class NewIrreversibleBlockFoundEventHandler : ILocalEventHandler<NewIrreversibleBlockFoundEvent>,
        ITransientDependency
    {
        public IBackgroundJobManager BackgroundJobManager { get; set; }

        public ILogger<NewIrreversibleBlockFoundEventHandler> Logger { get; set; }

        public NewIrreversibleBlockFoundEventHandler()
        {
            Logger = NullLogger<NewIrreversibleBlockFoundEventHandler>.Instance;
        }

        public async Task HandleEventAsync(NewIrreversibleBlockFoundEvent eventData)
        {
            await BackgroundJobManager.EnqueueAsync(new MergeBlockStateJobArgs
            {
                LastIrreversibleBlockHash = eventData.BlockHash.ToHex(),
                LastIrreversibleBlockHeight = eventData.BlockHeight
            });
        }
    }
}