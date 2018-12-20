using System.Collections.Generic;
using AElf.Synchronization.BlockSynchronization;
using Xunit;

namespace AElf.Synchronization.Tests
{
    public class BlockSetTests
    {
        [Fact]
        public void Init_WithGenesisBlock_LibIsGenesis()
        {
            var genesis = SyncTestHelpers.GetGenesisBlock();
            
            BlockSet blockSet = new BlockSet();
            blockSet.Init(new List<string>(), genesis);
            
            Assert.Equal(blockSet.CurrentLib.BlockHash, genesis.GetHash());
        }
    }
}