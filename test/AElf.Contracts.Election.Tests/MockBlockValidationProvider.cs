using System.Threading.Tasks;
using AElf.Kernel;
using AElf.Kernel.Blockchain.Application;

namespace AElf.Contracts.Election
{
    public class MockBlockValidationService : IBlockValidationService
    {
        public Task<bool> ValidateBlockBeforeAttachAsync(IBlock block)
        {
            return Task.FromResult(true);
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