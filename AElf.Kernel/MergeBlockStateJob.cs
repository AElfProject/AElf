using System.Threading.Tasks;
using AElf.Common;
using AElf.Kernel.SmartContract.Application;
using Volo.Abp.DependencyInjection;

namespace AElf.Kernel
{
    public class MergeBlockStateJob : AsyncBackgroundJob<MergeBlockStateJobArgs>, ITransientDependency
    {
        private readonly IBlockchainStateMergingService _blockchainStateMergingService;

        public MergeBlockStateJob(IBlockchainStateMergingService blockchainStateMergingService)
        {
            _blockchainStateMergingService = blockchainStateMergingService;
        }

        protected override async Task ExecuteAsync(MergeBlockStateJobArgs args)
        {
            await _blockchainStateMergingService.MergeBlockStateAsync(args.LastIrreversibleBlockHeight,
                Hash.LoadHex(args.LastIrreversibleBlockHash));
        }
    }
}