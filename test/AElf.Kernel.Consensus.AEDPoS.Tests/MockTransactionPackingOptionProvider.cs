using System.Threading.Tasks;
using AElf.Kernel.Txn.Application;

namespace AElf.Kernel.Consensus.DPoS.Tests
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