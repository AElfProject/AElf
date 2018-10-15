using System.Threading;
using System.Threading.Tasks;
using AElf.Kernel;

// ReSharper disable once CheckNamespace
namespace AElf.ChainController
{
    // ReSharper disable InconsistentNaming
    public class BlockExecutionService : IBlockExecutionService
    {
        /// <summary>
        /// Signals to a CancellationToken that mining should be canceled
        /// </summary>
        private CancellationTokenSource Cts { get; set; }
        
        public Task<BlockExecutionResultCC> ExecuteBlock(IBlock block)
        {
            throw new System.NotImplementedException();
        }

        public void Start()
        {
            Cts = new CancellationTokenSource();
        }
    }
}