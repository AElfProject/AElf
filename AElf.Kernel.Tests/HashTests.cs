using System.Collections.Generic;
using Xunit;

namespace AElf.Kernel.Tests
{
    public class HashTests
    {

        private Hash hash1 = new Hash(new byte[] { 10, 14, 1, 15 });
        private Hash hash2 = new Hash(new byte[] { 10, 14, 1, 15 });
        private Hash hash3 = new Hash(new byte[] { 15, 1, 14, 10 });
        private const string testValue = "test";


        [Fact]
        public void EqualTest()
        {
            Assert.True(hash1 == hash2);
            Assert.False(hash1 == hash3);
            Assert.False(hash2 == hash3);
        }

        [Fact]
        public void CompareTest()
        {
            Assert.True(new Hash().Compare(hash1, hash3) == 1);
            Assert.True(new Hash().Compare(hash1, hash2) == 0);
            Assert.True(new Hash().Compare(hash3, hash2) == 1);
        }

        [Fact]
        public void DictionaryTest()
        {
            var dict = new Dictionary<Hash, string>();
            dict[hash1] = testValue;
            Assert.True(dict.TryGetValue(hash2, out var test));
            Assert.Equal(test, testValue);
        }

        [Fact]
        public void RandomHashTest()
        {
            var hash1 = Hash.Generate();
            var hash2 = Hash.Generate();

            Assert.False(hash1 == hash2);
        }

        [Fact]
        public void PathTest()
        {
            var path = new Path();
            path.SetChainHash(Hash.Generate())
                .SetAccount(Hash.Generate())
                .SetDataProvider(Hash.Generate())
                .SetDataKey(Hash.Generate());

            Assert.False(path.IsPointer);
            Assert.NotNull(path.GetPathHash());
        }

        [Fact]
        public void PointerTest()
        {
            var path = new Path();
            path.SetChainHash(Hash.Generate())
                .SetBlockHash(Hash.Generate())
                .SetAccount(Hash.Generate())
                .SetDataProvider(Hash.Generate())
                .SetDataKey(Hash.Generate());

            Assert.True(path.IsPointer);
            Assert.NotNull(path.GetPointerHash());
        }
    }
}