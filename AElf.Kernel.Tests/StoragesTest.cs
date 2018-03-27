using AElf.Kernel.Storages;
using Xunit;
using Xunit.Frameworks.Autofac;

namespace AElf.Kernel.Tests
{
    [UseAutofacTestFramework]
    public class StoragesTest
    {
        private readonly IBlockStore _blockStore;

        public StoragesTest(IBlockStore blockStore)
        {
            _blockStore = blockStore;
        }
        
        [Fact]
        public void BlockStoreTest()
        {
            var block = new Block(Hash.Generate());
            block.AddTransaction(Hash.Generate());
            
            _blockStore.Insert(block);

            var hash = block.GetHash();
            var getBlock = _blockStore.GetAsync(hash).Result;
            
            Assert.True(block == getBlock);
        }
    }
}