using System.Threading.Tasks;
using Volo.Abp.BackgroundJobs;
using Volo.Abp.Threading;

namespace AElf.Kernel
{
    public abstract class AsyncBackgroundJob<TArgs> : BackgroundJob<TArgs>
    {
        public override void Execute(TArgs args)
        {
            AsyncHelper.RunSync(() => ExecuteAsync(args));
        }

        protected abstract Task ExecuteAsync(TArgs args);
    }
}