using System.Collections.Generic;
using System.Linq;
using AElf.Common;
using AElf.Kernel;
using AElf.Synchronization.BlockSynchronization;
using Xunit;

namespace AElf.Synchronization.Tests
{
    public class BlockSetTests
    {
        [Fact]
        public void PushBlock()
        {
            BlockSet blockSet = new BlockSet();
            List<IBlock> chain = SyncTestHelpers.GenerateChain(1);
            
            blockSet.PushBlock(chain.ElementAt(0)); // push genesis

            // push unlinkable
        }
    }
}