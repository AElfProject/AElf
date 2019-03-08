using System.Threading.Tasks;
using AElf.Kernel.Blockchain.Application;
using Volo.Abp.BackgroundJobs;
using Volo.Abp.Threading;

namespace AElf.OS.Jobs
{

    public abstract class AsyncBackgroundJob<TArgs> : BackgroundJob<TArgs>
    {
        public override void Execute(TArgs args)
        {
            AsyncHelper.RunSync(() => ExecuteAsync(args));
        }

        protected abstract Task ExecuteAsync(TArgs args);
    }
    
    public class AddBlockJob : AsyncBackgroundJob<string>
    {
        private IBlockchainService _blockchainService;

        public AddBlockJob(IBlockchainService blockchainService)
        {
            _blockchainService = blockchainService;
        }

        protected override async Task ExecuteAsync(string args)
        {
            
        }
    }
}