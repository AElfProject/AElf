using System.Threading.Tasks;
using AElf.Kernel.Blockchain.Application;

namespace AElf.Kernel.Miner.Application
{
    public class SystemTransactionValidationProvider : IBlockValidationProvider
    {
        private readonly ISystemTransactionExtraDataProvider _systemTransactionExtraDataProvider;

        public SystemTransactionValidationProvider(ISystemTransactionExtraDataProvider systemTransactionExtraDataProvider)
        {
            _systemTransactionExtraDataProvider = systemTransactionExtraDataProvider;
        }

        public Task<bool> ValidateBeforeAttachAsync(IBlock block)
        {
            return Task.FromResult(_systemTransactionExtraDataProvider.TryGetSystemTransactionCount(block.Header,
                                       out var systemTransactionCount) && systemTransactionCount > 0);
        }

        public Task<bool> ValidateBlockBeforeExecuteAsync(IBlock block)
        {
            return Task.FromResult(true);
        }

        public Task<bool> ValidateBlockAfterExecuteAsync(IBlock block)
        {
            return Task.FromResult(true);
        }
    }
}