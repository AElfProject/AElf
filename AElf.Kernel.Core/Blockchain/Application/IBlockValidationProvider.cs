using System.Threading.Tasks;

namespace AElf.Kernel.Blockchain.Application
{
    public interface IBlockValidationProvider
    {
        Task<bool> ValidateBlockBeforeExecuteAsync(int chainId, IBlock block);

        Task<bool> ValidateBlockAfterExecuteAsync(int chainId, IBlock block);
    }

    public class BlockValidationProvider : IBlockValidationProvider
    {
        public async Task<bool> ValidateBlockBeforeExecuteAsync(int chainId, IBlock block)
        {
            throw new System.NotImplementedException();
        }

        public async Task<bool> ValidateBlockAfterExecuteAsync(int chainId, IBlock block)
        {
            // TODO: validate state merkel tree
            
            throw new System.NotImplementedException();
        }
    }
}