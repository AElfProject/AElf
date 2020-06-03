using System.Threading.Tasks;
using AElf.Kernel;
using AElf.Kernel.Txn.Application;

namespace AElf.CrossChain
{
    public class MockTransactionPackingOptionProvider : ITransactionPackingOptionProvider
    {
        private bool _isTransactionPackable = true;

        public Task SetTransactionPackingOptionAsync(IBlockIndex blockIndex, bool isTransactionPackable)
        {
            _isTransactionPackable = isTransactionPackable;
            return Task.CompletedTask;
        }

        public bool IsTransactionPackable(IChainContext chainContext)
        {
            return _isTransactionPackable;
        }
    }
}