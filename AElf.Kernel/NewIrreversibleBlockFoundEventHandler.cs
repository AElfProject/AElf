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
        private readonly IBackgroundJobManager _backgroundJobManager;

        public NewIrreversibleBlockFoundEventHandler(IBackgroundJobManager backgroundJobManager)
        {
            _backgroundJobManager = backgroundJobManager;
            Logger = NullLogger<NewIrreversibleBlockFoundEventHandler>.Instance;
        }

        public ILogger<NewIrreversibleBlockFoundEventHandler> Logger { get; set; }

        public async Task HandleEventAsync(NewIrreversibleBlockFoundEvent eventData)
        {
            await _backgroundJobManager.EnqueueAsync(new MergeBlockStateJobArgs
            {
                LastIrreversibleBlockHash = eventData.BlockHash.ToHex(),
                LastIrreversibleBlockHeight = eventData.BlockHeight
            });
        }
    }
}