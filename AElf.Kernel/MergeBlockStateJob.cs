using System.Threading.Tasks;
using AElf.Common;
using AElf.Kernel.SmartContract.Application;
using Volo.Abp.DependencyInjection;

namespace AElf.Kernel
{
    public class MergeBlockStateJob : AsyncBackgroundJob<MergeBlockStateJobArgs>, ITransientDependency
    {
        public IBlockchainStateMergingService BlockchainStateMergingService { get; set; }

        protected override async Task ExecuteAsync(MergeBlockStateJobArgs args)
        {
            await BlockchainStateMergingService.MergeBlockStateAsync(args.LastIrreversibleBlockHeight,
                Hash.LoadHex(args.LastIrreversibleBlockHash));
        }
    }
}