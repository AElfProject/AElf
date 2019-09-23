using Microsoft.Extensions.Options;
using Volo.Abp.BackgroundWorkers;
using Volo.Abp.Threading;

namespace AElf.OS.Network.Worker
{
    public class PeerReconnectionWorker : PeriodicBackgroundWorkerBase
    {
        public PeerReconnectionWorker(AbpTimer timer, IOptionsSnapshot<NetworkOptions> networkOptions)
            : base(timer)
        {

        }

        protected override void DoWork()
        {
            // todo implement reconnection logic
        }
    }
}